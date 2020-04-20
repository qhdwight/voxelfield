using System;
using System.Collections.Generic;

namespace Swihoni.Collections
{
    public class Pool<T> where T : class
    {
        private readonly Stack<T> m_Pool;
        private readonly LinkedList<T> m_InUse;
        private readonly Func<T> m_Constructor;
        private readonly Action<T, bool> m_UsageChanged;

        public int Remaining => m_Pool.Count;

        public Pool(int capacity, Func<T> constructor, Action<T, bool> usageChanged = null)
        {
            m_Constructor = constructor;
            m_UsageChanged = usageChanged;
            m_Pool = new Stack<T>(capacity);
            m_InUse = new LinkedList<T>();
            for (var i = 0; i < capacity; i++)
                Return(constructor());
        }

        public void Return(T toReturn)
        {
            m_UsageChanged?.Invoke(toReturn, false);
            m_InUse.Remove(toReturn);
            m_Pool.Push(toReturn);
        }

        public void ReturnAll()
        {
            foreach (T item in m_InUse)
            {
                m_UsageChanged?.Invoke(item, false);
                m_Pool.Push(item);
            }
            m_InUse.Clear();
        }

        public T Obtain()
        {
            T obtainedItem = m_Pool.Count > 0 ? m_Pool.Pop() : GetItemWhenEmpty();
            m_UsageChanged?.Invoke(obtainedItem, true);
            m_InUse.AddLast(obtainedItem);
            return obtainedItem;
        }

        protected virtual T GetItemWhenEmpty()
        {
            return m_Constructor();
        }
    }
}