using System;

namespace Swihoni.Collections    
{
    public class StrictPool<T> : Pool<T> where T : class
    {
        private readonly Action<T> m_ForcePopped;

        public StrictPool(int capacity, Func<T> constructor, Action<T> forcePopped = null) : base(capacity, constructor) => m_ForcePopped = forcePopped;

        protected override T GetItemWhenEmpty()
        {
            T obtainedItem = m_InUse.First.Value;
            m_InUse.RemoveFirst();
            m_ForcePopped?.Invoke(obtainedItem);
            return obtainedItem;
        }
    }
}