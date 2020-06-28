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

        public abstract ElementBase GetValue(int index);
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

        public override ElementBase GetValue(int index) => this[index];

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
    public class StringProperty : PropertyBase
    {
        private readonly int m_MaxSize;
        private const int DefaultSize = 255;

        public StringBuilder Builder { get; }

        public StringProperty() : this(DefaultSize) { }

        public StringProperty(int maxSize)
        {
            m_MaxSize = maxSize;
            Builder = new StringBuilder(maxSize);
        }

        public StringProperty(string @string, int maxSize = DefaultSize) : this(maxSize) => SetTo(@string);

        private void ThrowIfOverMaxSize() => ThrowIfOverMaxSize(Builder.Length);

        private void ThrowIfOverMaxSize(int size)
        {
            if (size > m_MaxSize) throw new Exception($"String was over max size! Size: {size} Max: {m_MaxSize}");
        }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put((byte) Builder.Length);
            for (var i = 0; i < Builder.Length; i++)
                writer.Put(Builder[i]);
        }

        public override void Deserialize(NetDataReader reader)
        {
            int size = reader.GetByte();
            ThrowIfOverMaxSize(size);
            Zero();
            for (var _ = 0; _ < size; _++)
                Builder.Append(reader.GetChar());
            WithValue = true;
        }
        
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = base.GetHashCode();
                hashCode = (hashCode * 397) ^ m_MaxSize;
                hashCode = (hashCode * 397) ^ (Builder != null ? Builder.GetHashCode() : 0);
                return hashCode;
            }
        }

        public override bool Equals(object other) => this == (StringProperty) other;

        public override bool Equals(PropertyBase other) => this == (StringProperty) other;

        public static bool operator ==(StringProperty s1, StringProperty s2)
        {
            if (s1.Builder.Length != s2.Builder.Length) return false;
            for (var i = 0; i < s1.Builder.Length; i++)
                if (s1.Builder[i] != s2.Builder[i])
                    return false;
            return true;
        }

        public static bool operator !=(StringProperty s1, StringProperty s2) => !(s1 == s2);

        public override void Zero() => Builder.Clear();

        public override void SetFromIfWith(PropertyBase other)
        {
            if (other.WithValue) SetTo(other);
        }

        public override void SetTo(PropertyBase other)
        {
            if (!(other is StringProperty otherString)) throw new ArgumentException("Other property is not a string!");
            ThrowIfOverMaxSize(otherString.Builder.Length);
            Zero();
            Builder.Append(otherString.Builder);
            WithValue = true;
        }

        public void SetTo(string @string)
        {
            ThrowIfOverMaxSize(@string.Length);
            Zero();
            Builder.Append(@string);
            WithValue = true;
        }

        public void SetTo(Action<StringBuilder> action)
        {
            Zero();
            action(Builder);
            ThrowIfOverMaxSize();
            WithValue = true;
        }

        public override void InterpolateFromIfWith(PropertyBase p1, PropertyBase p2, float interpolation) => SetTo(p2);

        public override string ToString() => Builder.ToString();
    }
}