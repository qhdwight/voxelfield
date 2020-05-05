using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
        [CopyField, SerializeField] protected T[] m_Values;

        public ArrayProperty(params T[] values) => m_Values = (T[]) values.Clone();

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

        public IEnumerator<T> GetEnumerator() => ((IEnumerable<T>) m_Values).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Values.GetEnumerator();

        public override object GetValue(int index) => this[index];

        public override Type GetElementType() => typeof(T);
    }

    [Serializable]
    public class CharProperty : PropertyBase<char>
    {
        public override bool ValueEquals(PropertyBase<char> other) => other.Value == Value;

        public override void SerializeValue(BinaryWriter writer) => writer.Write(Value);

        public override void DeserializeValue(BinaryReader reader) => Value = reader.ReadChar();
    }

    [Serializable]
    public class StringProperty : ArrayProperty<CharProperty>
    {
        private StringBuilder m_Builder;

        public StringProperty(int size) : base(size) => m_Builder = new StringBuilder(size);

        public StringBuilder GetString()
        {
            m_Builder.Clear();
            foreach (char character in m_Values)
            {
                if (character == '\0')
                    break;
                m_Builder.Append(character);
            }
            return m_Builder;
        }

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
                    m_Values[i].Value = '\0';
                    break;
                }
            }
        }
    }
}