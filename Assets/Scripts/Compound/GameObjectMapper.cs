using System;
using System.Collections.Generic;
using System.Linq;

namespace Compound
{
    public class GameObjectMapper<T1, T2> : Pool<T2> where T2 : class
    {
        private readonly Dictionary<T1, T2> m_Dictionary = new Dictionary<T1, T2>();

        public GameObjectMapper(int capacity, Func<T2> constructor, Action<T2, bool> usageChanged) : base(capacity, constructor, usageChanged)
        {
        }

        public void Evaluate(HashSet<T1> list, Action<T1, T2> action)
        {
            foreach (T1 key in m_Dictionary.Keys.ToArray())
            {
                if (list.Contains(key)) continue;
                Return(m_Dictionary[key]);
                m_Dictionary.Remove(key);
            }
            foreach (T1 t1 in list)
            {
                if (!m_Dictionary.ContainsKey(t1))
                    m_Dictionary.Add(t1, Obtain());
                action(t1, m_Dictionary[t1]);
            }
        }
    }
}