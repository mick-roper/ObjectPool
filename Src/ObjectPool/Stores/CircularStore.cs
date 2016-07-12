using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObjectPool.Stores
{
    sealed class CircularStore<T> : IObjectStore<T> where T : class
    {
        readonly List<Slot<T>> slots_;
        int freeSlotCount;
        int position = -1;

        public CircularStore(int capacity)
        {
            slots_ = new List<Slot<T>>(capacity);
        }

        public int Count { get { return freeSlotCount; } }

        public T Fetch()
        {
            if (Count == 0) throw new InvalidOperationException("The store is empty!");

            var startPos = position;

            do
            {
                Advance();

                var slot = slots_[position];

                if (!slot.IsInUse)
                {
                    slot.IsInUse = true;
                    --freeSlotCount;
                    return slot.Item;
                }
            }
            while (startPos != position);

            // if we get this far, somthing went wrong!
            throw new InvalidOperationException("No free slots!");
        }

        public void Add(T instance)
        {
            var slot = slots_.Find(x => object.Equals(x.Item, instance));

            if (slot == null)
            {
                slot = new Slot<T>(instance);
                slots_.Add(slot);
            }

            slot.IsInUse = false;
            ++freeSlotCount;
        }

        private void Advance()
        {
            position = (position + 1) % slots_.Count;
        }
    }
}
