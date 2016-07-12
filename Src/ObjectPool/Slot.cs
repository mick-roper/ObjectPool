using System;

namespace ObjectPool
{
    internal class Slot<T> where T : class
    {
        readonly T _item;

        public Slot(T item)
        {
            if (item == null) throw new ArgumentNullException("item");

            _item = item;
            IsInUse = false;
        }

        public T Item { get { return _item; } }
        public bool IsInUse { get; set; }
    }
}