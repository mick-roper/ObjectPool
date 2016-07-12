using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectPool
{
    public enum AccessMode
    {
        /// <summary>
        /// First in, First out
        /// </summary>
        Fifo,
        /// <summary>
        /// Last in, first out
        /// </summary>
        Lifo,
        /// <summary>
        /// Circular (or round-robin)
        /// </summary>
        Circular
    }
}
