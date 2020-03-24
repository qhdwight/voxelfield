using System;
using System.Collections.Generic;
using System.Linq;

namespace Compound
{
    public class Mapper<TKey, TValue> : Pool<TValue> where TValue : class
    {
        private readonly Dictionary<TKey, TValue> m_Dictionary = new Dictionary<TKey, TValue>();

        public Mapper(int capacity, Func<TValue> constructor, Action<TValue, bool> usageChanged) : base(capacity, constructor, usageChanged)
        {
        }

        public void Synchronize(HashSet<TKey> list)
        {
            foreach (TKey key in m_Dictionary.Keys.ToArray())
            {
                if (list.Contains(key)) continue;
                Return(m_Dictionary[key]);
                m_Dictionary.Remove(key);
            }
            foreach (TKey key in list.Where(key => !m_Dictionary.ContainsKey(key)))
                m_Dictionary.Add(key, Obtain());
        }

        public void Synchronize(HashSet<TKey> list, Action<TKey, TValue> action)
        {
            Synchronize(list);
            foreach (KeyValuePair<TKey, TValue> pair in m_Dictionary)
                action(pair.Key, pair.Value);
        }

        public void Execute(TKey key, Action<TValue> action)
        {
            action(m_Dictionary[key]);
        }
    }
}