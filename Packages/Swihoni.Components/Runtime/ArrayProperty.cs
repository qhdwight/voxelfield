using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Swihoni.Components
{
    public abstract class ArrayPropertyBase : ElementBase
    {
        public abstract int Length { get; }

        public abstract object GetValue(int index);

        public abstract Type GetElementType();
    }

    [Serializable]
    public class ArrayProperty<T> : ArrayPropertyBase, IEnumerable<T> where T : ElementBase, new()
    {
        [CopyField, SerializeField] private T[] m_Values;

        public ArrayProperty(params T[] values)
        {
            m_Values = (T[]) values.Clone();
        }

        public ArrayProperty(int size)
        {
            m_Values = new T[size];
            SetAll(() => new T());
        }

        public void SetAll(Func<T> constructor)
        {
            for (var i = 0; i < m_Values.Length; i++)
                m_Values[i] = constructor();
        }

        public T this[int index]
        {
            get => m_Values[index];
            set => m_Values[index] = value;
        }

        public override int Length => m_Values.Length;

        public IEnumerator<T> GetEnumerator()
        {
            return ((IEnumerable<T>) m_Values).GetEnumerator();
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
    }
}