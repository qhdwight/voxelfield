using System.Collections.Generic;

namespace Swihoni.Collections
{
    public class DualDictionary<TKey, TValue>
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
            foreach ((TKey key, TValue value) in original)
            {
                m_Forward.Add(key, value);
                m_Reverse.Add(value, key);
            }
        }

        public void Add(TKey key, TValue value)
        {
            m_Forward.Add(key, value);
            m_Reverse.Add(value, key);
        }

        public bool ContainsForward(TKey key) => m_Forward.ContainsKey(key);

        public bool HasBackward(TValue value) => m_Reverse.ContainsKey(value);

        public TValue GetForward(TKey key) => m_Forward[key];

        public bool ContainsReverse(TValue key) => m_Reverse.ContainsKey(key);

        public TKey GetReverse(TValue value) => m_Reverse[value];

        public void Remove(TValue value)
        {
            m_Forward.Remove(m_Reverse[value]);
            m_Reverse.Remove(value);
        }

        public void Remove(TKey key)
        {
            m_Reverse.Remove(m_Forward[key]);
            m_Forward.Remove(key);
        }

        public bool TryGetForward(TKey key, out TValue value) => m_Forward.TryGetValue(key, out value);

        public bool TryGetReverse(TValue value, out TKey key) => m_Reverse.TryGetValue(value, out key);
    }
}