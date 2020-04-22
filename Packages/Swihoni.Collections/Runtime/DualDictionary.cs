using System.Collections;
using System.Collections.Generic;

namespace Swihoni.Collections
{
    public class DualDictionary<TKey, TValue> : IEnumerable<(TKey, TValue)>
    {
        private readonly Dictionary<TKey, TValue> m_Forward;
        private readonly Dictionary<TValue, TKey> m_Reverse;

        public Dictionary<TKey, TValue>.KeyCollection Forwards => m_Forward.Keys;
        public Dictionary<TValue, TKey>.KeyCollection Backwards => m_Reverse.Keys;

        public int Length => m_Forward.Count;

        public DualDictionary(int capacity = 0)
        {
            m_Forward = new Dictionary<TKey, TValue>(capacity);
            m_Reverse = new Dictionary<TValue, TKey>(capacity);
        }

        public DualDictionary(Dictionary<TKey, TValue> original)
        {
            m_Forward = new Dictionary<TKey, TValue>(original.Count);
            m_Reverse = new Dictionary<TValue, TKey>(original.Count);
            foreach (KeyValuePair<TKey, TValue> pair in original)
            {
                m_Forward.Add(pair.Key, pair.Value);
                m_Reverse.Add(pair.Value, pair.Key);
            }
        }

        public void Add(TKey key, TValue value)
        {
            m_Forward.Add(key, value);
            m_Reverse.Add(value, key);
        }

        public bool ContainsForward(TKey key)
        {
            return m_Forward.ContainsKey(key);
        }

        public bool HasBackward(TValue value)
        {
            return m_Reverse.ContainsKey(value);
        }

        public TValue GetForward(TKey key)
        {
            return m_Forward[key];
        }

        public bool ContainsReverse(TValue key)
        {
            return m_Reverse.ContainsKey(key);
        }

        public TKey GetReverse(TValue value)
        {
            return m_Reverse[value];
        }

        public void Remove(TValue entry)
        {
            m_Forward.Remove(m_Reverse[entry]);
            m_Reverse.Remove(entry);
        }

        public IEnumerator<(TKey, TValue)> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> pair in m_Forward)
            {
                yield return (pair.Key, pair.Value);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}