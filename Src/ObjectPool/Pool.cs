using ObjectPool.Stores;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ObjectPool
{
    /// <summary>
    /// An object pool instance.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public sealed class Pool<T> : IDisposable where T : class
    {
        readonly IObjectStore<T> store_;
        readonly LoadingMode loadingMode_;
        readonly Func<Pool<T>, T> instanceFactory_;
        readonly Semaphore sync_;

        bool disposed_;
        int size_;
        int count_;

        /// <summary>
        /// Creates a new instance of <see cref="Pool{T}"/>
        /// </summary>
        /// <param name="size">The size of the pool</param>
        /// <param name="loadingMode">The loading mode the pool should use</param>
        /// <param name="accessMode">The access mode the pool should use</param>
        /// <param name="instanceFactory">An delegate that builds instances of <see cref="T"/> </param>
        public Pool(int size, LoadingMode loadingMode, AccessMode accessMode, Func<Pool<T>, T> instanceFactory)
        {
            if (size <= 0) throw new ArgumentOutOfRangeException("size must be greater than 0!");
            if (instanceFactory == null) throw new ArgumentNullException("instanceFactory");

            disposed_ = false;
            size_ = size;
            loadingMode_ = loadingMode;
            instanceFactory_ = instanceFactory;
            sync_ = new Semaphore(size_, size_);
            store_ = CreateStore(accessMode, size_);

            if (loadingMode == LoadingMode.Eager)
            {
                PreLoad();
            }
        }

        #region private static members
        /// <summary>
        /// Creates an underlying store instance
        /// </summary>
        /// <param name="mode">The access mode that the store should use</param>
        /// <param name="capacity">The capacity of the store</param>
        /// <returns></returns>
        private static IObjectStore<T> CreateStore(AccessMode mode, int capacity)
        {
            switch (mode)
            {
                case AccessMode.Fifo:
                    return new QueueStore<T>(capacity);
                case AccessMode.Lifo:
                    return new StackStore<T>(capacity);
                default:
                    return new CircularStore<T>(capacity);
            }
        } 
        #endregion

        #region private instance members
        /// <summary>
        /// Eagrely acquires an instance of T
        /// </summary>
        /// <returns></returns>
        private T AcquireEagre()
        {
            lock (store_)
            {
                return store_.Fetch();
            }
        }

        /// <summary>
        /// Lazily acquires an instance of T
        /// </summary>
        /// <returns></returns>
        private T AcquireLazy()
        {
            lock (store_)
            {
                if (store_.Count > 0)
                {
                    return store_.Fetch();
                }
            }

            Interlocked.Increment(ref count_);
            return instanceFactory_(this);
        }

        /// <summary>
        /// Acquires an instance of T using a hybrid of eagre and lazy loading
        /// </summary>
        /// <returns></returns>
        private T AcquireHybrid()
        {
            bool shouldExpand = false;
            if (count_ < size_)
            {
                var newCount = Interlocked.Increment(ref count_);
                if (newCount <= size_)
                {
                    shouldExpand = true;
                }
                else
                {
                    // another thread must have taken the last spot - switch to the store!
                    Interlocked.Decrement(ref count_);
                }
            }

            if (shouldExpand)
            {
                return instanceFactory_(this);
            }
            else
            {
                return AcquireEagre();
            }
        }

        /// <summary>
        /// Pre0loads the store with items
        /// </summary>
        private void PreLoad()
        {
            for (int i = 0; i < size_; i++)
            {
                var item = instanceFactory_(this);
                store_.Add(item);
            }

            count_ = size_;
        }
        #endregion

        #region public instance members
        /// <summary>
        /// Checks if the pool has been disposed
        /// </summary>
        public bool IsDisposed { get { return disposed_; } }

        /// <summary>
        /// Acquires an instance from the store
        /// </summary>
        /// <returns></returns>
        public T Acquire()
        {
            sync_.WaitOne();
            switch (loadingMode_)
            {
                case LoadingMode.Eager:
                    return AcquireEagre();
                case LoadingMode.Lazy:
                    return AcquireLazy();
                case LoadingMode.Hybrid:
                default:
                    return AcquireHybrid();
            }
        }

        /// <summary>
        /// Releases a lock on an object and returns it to the store
        /// </summary>
        /// <param name="instance">The instance</param>
        public void Release(T instance)
        {
            lock (store_)
            {
                store_.Add(instance);
            }

            sync_.Release();
        }

        /// <summary>
        /// Releases all object managed by this pool
        /// </summary>
        public void Dispose()
        {
            if (disposed_)
            {
                return;
            }

            disposed_ = true;

            if (typeof(IDisposable).IsAssignableFrom(typeof(T)))
            {
                lock(store_)
                {
                    while (store_.Count > 0)
                    {
                        var disposable = store_.Fetch() as IDisposable;
                        disposable.Dispose();
                    }
                }
            }

            sync_.Close();
        } 
        #endregion
    }
}
