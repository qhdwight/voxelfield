using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Collections
{
    public class Pool<T> : IEnumerable<T>, IDisposable where T : class
    {
        protected readonly LinkedList<T> m_Pool, m_InUse;
        private readonly Func<T> m_Constructor;
        private readonly Action<T, bool> m_UsageChanged;

        public IEnumerable<T> InUse => m_InUse;

        public Pool(int capacity, Func<T> constructor, Action<T, bool> usageChanged = null)
        {
            m_Constructor = constructor;
            m_UsageChanged = usageChanged;
            m_Pool = new LinkedList<T>();
            m_InUse = new LinkedList<T>();
            for (var i = 0; i < capacity; i++)
                Return(constructor());
        }

        public T RemoveAndObtain(T existing)
        {
            if (m_InUse.Remove(existing))
            {
                if (existing is IDisposable disposable) disposable.Dispose();
                if (existing is Component component) UnityObject.DestroyImmediate(component.transform.root.gameObject);   
            }
            return Obtain();
        }

        public void Return(T toReturn)
        {
            m_UsageChanged?.Invoke(toReturn, false);
            m_InUse.Remove(toReturn);
            m_Pool.AddLast(toReturn);
        }

        public void ReturnAll()
        {
            foreach (T item in m_InUse)
            {
                m_UsageChanged?.Invoke(item, false);
                m_Pool.AddLast(item);
            }
            m_InUse.Clear();
        }

        private T Get()
        {
            T last;
            do
            {
                if (m_Pool.Count == 0) return GetItemWhenEmpty();
                last = m_Pool.Last.Value;
                if (last == null) Debug.LogWarning("Null object in pool");
                m_Pool.RemoveLast();
            } while (last == null);
            return last;
        }

        public T Obtain()
        {
            T obtainedItem = Get();
            m_UsageChanged?.Invoke(obtainedItem, true);
            m_InUse.AddLast(obtainedItem);
            return obtainedItem;
        }

        protected virtual T GetItemWhenEmpty() => m_Constructor();

        public IEnumerator<T> GetEnumerator() => m_Pool.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Pool.GetEnumerator();

        public void Dispose()
        {
            foreach (T element in m_Pool)
                if (element is IDisposable disposable)
                    disposable.Dispose();
        }
    }
}