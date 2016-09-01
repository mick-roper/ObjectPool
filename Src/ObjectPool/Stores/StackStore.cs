using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectPool.Stores
{
    sealed class StackStore<T> : Stack<T>, IObjectStore<T> where T : class
    {
        public StackStore(int capacity) : base(capacity) { }

        public T Fetch()
        {
            return Pop();
        }

        public void Add(T instance)
        {
            Push(instance);
        }
    }
}
