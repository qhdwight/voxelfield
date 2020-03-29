using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Components
{
    public class ArrayProperty<T> : IEnumerable<T>
    {
        [Copy] private readonly T[] m_Values;

        private ArrayProperty(T[] values)
        {
            m_Values = values;
        }

        public ArrayProperty(int size)
        {
            m_Values = Enumerable.Range(1, size).Select(_ => Activator.CreateInstance<T>()).ToArray();
        }

        public T this[int index]
        {
            get => m_Values[index];
            set => m_Values[index] = value;
        }
        public int Length => m_Values.Length;

        public IEnumerator<T> GetEnumerator()
        {
            return m_Values.OfType<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Values.GetEnumerator();
        }

        public static implicit operator ArrayProperty<T>(T[] values)
        {
            return new ArrayProperty<T>(values);
        }
    }
}