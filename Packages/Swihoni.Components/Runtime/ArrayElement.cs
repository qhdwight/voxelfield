using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Components
{
    public abstract class ArrayElementBase : ElementBase
    {
        public abstract int Length { get; }

        public abstract Type GetElementType { get; }

        public abstract ElementBase GetValue(int index);
        
        public ElementBase this[int index] => GetValue(index);
    }

    [Serializable]
    public class ArrayElement<T> : ArrayElementBase, IEnumerable<T> where T : ElementBase, new()
    {
        [CopyField, SerializeField] protected T[] m_Values;

        public ArrayElement(params T[] values) => m_Values = (T[]) values.Clone();

        public ArrayElement(int size)
        {
            m_Values = new T[size];
            SetAll(() => new T());
        }

        public void SetAll(Func<T> constructor)
        {
            for (var i = 0; i < m_Values.Length; i++)
                m_Values[i] = constructor();
        }

        public new T this[int index]
        {
            get => m_Values[index];
            set => m_Values[index] = value;
        }

        public override int Length => m_Values.Length;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>) m_Values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Values.GetEnumerator();

        public override ElementBase GetValue(int index) => this[index];

        public override Type GetElementType => typeof(T);
    }

    [Serializable]
    public class CharProperty : PropertyBase<char>
    {
        public override bool ValueEquals(in char value) => value == Value;
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetChar();
    }
}