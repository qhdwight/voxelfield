using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Components
{
    public abstract class ArrayElementBase : ElementBase
    {
        public abstract int Length { get; }

        public abstract Type GetElementType { get; }

        public abstract object GetValue(int index);
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

        public T this[int index]
        {
            get => m_Values[index];
            set => m_Values[index] = value;
        }

        public override int Length => m_Values.Length;

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>) m_Values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Values.GetEnumerator();

        public override object GetValue(int index) => this[index];

        public override Type GetElementType => typeof(T);
    }

    [Serializable]
    public class CharProperty : PropertyBase<char>
    {
        public override bool ValueEquals(PropertyBase<char> other) => other.Value == Value;
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetChar();
    }

    [Serializable]
    public class StringElement : ArrayElement<CharProperty>
    {
        private StringBuilder m_Builder;

        public StringElement(int size) : base(size) => m_Builder = new StringBuilder(size);

        public bool WithValue => m_Values[0].WithValue;

        public StringBuilder GetString()
        {
            m_Builder.Clear();
            foreach (CharProperty character in m_Values)
            {
                if (character.WithoutValue)
                    break;
                m_Builder.Append(character);
            }
            return m_Builder;
        }

        /// <summary>
        /// Modifies internal character array via a string builder.
        /// </summary>
        /// <param name="action">Use append methods on string builder to chain.</param>
        public void SetString(Action<StringBuilder> action)
        {
            m_Builder.Clear();
            action(m_Builder);
            for (var i = 0; i < m_Builder.Capacity; i++)
            {
                if (i < m_Builder.Length)
                    m_Values[i].Value = m_Builder[i];
                else
                {
                    m_Values[i].Clear();
                    break;
                }
            }
        }
    }
}