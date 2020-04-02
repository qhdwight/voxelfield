using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Components
{
    public abstract class ArrayPropertyBase
    {
        public abstract int Length { get; }

        public abstract object GetValue(int index);

        public abstract Type GetElementType();
    }

    [Serializable]
    public class ArrayProperty<T> : ArrayPropertyBase, IEnumerable<T>
    {
        [CopyField, SerializeField] private T[] m_Values;

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

        public override int Length => m_Values.Length;

        public IEnumerator<T> GetEnumerator()
        {
            return m_Values.OfType<T>().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Values.GetEnumerator();
        }

        public override object GetValue(int index)
        {
            return this[index];
        }

        public override Type GetElementType()
        {
            return typeof(T);
        }

        public static ArrayProperty<T> From(params T[] elements)
        {
            return new ArrayProperty<T>(elements);
        }

        public static implicit operator ArrayProperty<T>(T[] values)
        {
            return new ArrayProperty<T>(values);
        }
    }
}