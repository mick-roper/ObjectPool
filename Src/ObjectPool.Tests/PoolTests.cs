using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Threading.Tasks;

namespace ObjectPool.Tests
{
    [TestClass]
    public class PoolTests
    {
        [TestMethod]
        public void Pool_is_sealed()
        {
            var isSealed = typeof(Pool<>).IsSealed;

            Assert.IsTrue(isSealed);
        }

        [TestMethod]
        public void Pool_implements_IDisposable()
        {
            var implements = typeof(IDisposable).IsAssignableFrom(typeof(Pool<>));

            Assert.IsTrue(implements);
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void Pool_ctor_throws_if_size_is_less_than_1()
        {
            using (var pool = new Pool<TestObject>(0, LoadingMode.Lazy, AccessMode.Circular, () => { return new TestObject(); })) { }
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void Pool_ctor_throws_if_factory_is_null()
        {
            using (var pool = new Pool<TestObject>(1, LoadingMode.Lazy, AccessMode.Circular, null)) { }
        }

        [TestMethod]
        public void Pool_ctor_initialises_store_with_multiple_items_if_eagre_loading_selected()
        {
            const int expectedItems = 10;
            const LoadingMode lMode = LoadingMode.Eager;
            const AccessMode aMode = AccessMode.Fifo;

            Func<TestObject> factory = () => { return new TestObject(); };

            using (var pool = new Pool<TestObject>(expectedItems, lMode, aMode, factory))
            {
                Assert.AreEqual(expectedItems, pool.Count);
            }
        }

        [TestMethod]
        public void Pool_Acquire_returns_one_fifo_instance_shared_by_many_threads()
        {
            const int items = 1;
            const LoadingMode lMode = LoadingMode.Eager;
            const AccessMode aMode = AccessMode.Fifo;

            Func<TestObject> factory = () => { return new TestObject(); };

            using (var pool = new Pool<TestObject>(items, lMode, aMode, factory))
            {
                Action taskFunc = () => {
                    var i = pool.Acquire();

                    ++i.Activations;

                    Task.Delay(500).Wait();

                    pool.Release(i);
                };

                var tasks = new[] {
                    Task.Run(taskFunc),
                    Task.Run(taskFunc),
                    Task.Run(taskFunc),
                };

                Task.WaitAll(tasks);

                var instance = pool.Acquire();

                Assert.AreEqual(tasks.Length, instance.Activations);
            }
        }

        [TestMethod]
        public void Pool_Acquire_returns_one_lifo_instance_shared_by_many_threads()
        {
            const int items = 1;
            const LoadingMode lMode = LoadingMode.Lazy;
            const AccessMode aMode = AccessMode.Lifo;

            Func<TestObject> factory = () => { return new TestObject(); };

            using (var pool = new Pool<TestObject>(items, lMode, aMode, factory))
            {
                Action taskFunc = () => {
                    var i = pool.Acquire();

                    ++i.Activations;

                    // lock the thread for a little while!
                    Task.Delay(500).Wait();

                    pool.Release(i);
                };

                var tasks = new[] {
                    Task.Run(taskFunc),
                    Task.Run(taskFunc),
                    Task.Run(taskFunc),
                };

                Task.WaitAll(tasks);

                var instance = pool.Acquire();

                Assert.AreEqual(tasks.Length, instance.Activations);
            }
        }

        [TestMethod]
        public void Pool_Acquire_returns_one_circular_instance_shared_by_many_threads()
        {
            const int items = 1;
            const LoadingMode lMode = LoadingMode.Hybrid;
            const AccessMode aMode = AccessMode.Circular;

            Func<TestObject> factory = () => { return new TestObject(); };

            using (var pool = new Pool<TestObject>(items, lMode, aMode, factory))
            {
                Action taskFunc = () => {
                    var i = pool.Acquire();

                    ++i.Activations;

                    Task.Delay(500).Wait();

                    pool.Release(i);
                };

                var tasks = new[] {
                    Task.Run(taskFunc),
                    Task.Run(taskFunc),
                    Task.Run(taskFunc),
                };

                Task.WaitAll(tasks);

                var instance = pool.Acquire();

                Assert.AreEqual(tasks.Length, instance.Activations);

                pool.Release(instance);
            }
        }

        [TestMethod]
        public void Pool_Acquire_returns_multiple_circular_instances_allowing_concurrent_operation()
        {
            const int count = 10;
            const LoadingMode lMode = LoadingMode.Hybrid;
            const AccessMode aMode = AccessMode.Circular;

            Func<TestObject> factory = () => { return new TestObject(); };

            int i;

            using (var pool = new Pool<TestObject>(count, lMode, aMode, factory))
            {
                Action taskFunc = () => {
                    var obj = pool.Acquire();

                    ++obj.Activations;

                    Task.Delay(500).Wait();

                    pool.Release(obj);
                };

                var tasks = new Task[count];

                for (i = 0; i < tasks.Length; i++)
                {
                    tasks[i] = Task.Run(taskFunc);
                }

                Task.WaitAll(tasks);

                var instances = new TestObject[count];

                for (i = 0; i < instances.Length; i++)
                {
                    var instance = instances[i] = pool.Acquire();

                    Assert.AreEqual(1, instance.Activations);

                    pool.Release(instances[i]);
                }
            }
        }

        [TestMethod]
        public void Pool_Dispose_does_not_throw_error_if_called_more_than_once()
        {
            var pool = new Pool<TestObject>(1, LoadingMode.Eager, AccessMode.Circular, () => { return new TestObject(); });

            // we shouldnt get an 'object disposed' exception
            pool.Dispose();

            Assert.IsTrue(pool.IsDisposed);

            pool.Dispose();
        }

        sealed class TestObject : IDisposable
        {
            public int Activations { get; set; }

            public bool Disposed { get; private set; }

            public void Dispose()
            {
                Disposed = true;
            }
        }
    }
}
