using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectPool.Stores
{
    sealed class QueueStore<T> : Queue<T>, IObjectStore<T> where T : class
    {
        public QueueStore(int capacity) : base(capacity)
        {

        }

        public T Fetch()
        {
            return Dequeue();
        }

        public void Add(T instance)
        {
            Enqueue(instance);
        }
    }
}
