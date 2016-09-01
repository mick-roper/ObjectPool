using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectPool.Stores
{
    /// <summary>
    /// An interface that describes an object store
    /// </summary>
    interface IObjectStore<T> where T : class
    {
        /// <summary>
        /// Fetches an item from the store
        /// </summary>
        /// <returns></returns>
        T Fetch();
        /// <summary>
        /// Adds an instance to the store
        /// </summary>
        /// <param name="instance">The instance</param>
        void Add(T instance);
        /// <summary>
        /// Gets a count of items in the store
        /// </summary>
        int Count { get; }
    }
}
