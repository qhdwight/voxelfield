using System;
using System.Text;
using LiteNetLib.Utils;

namespace Swihoni.Components
{
    [Serializable]
    public class StringProperty : PropertyBase
    {
        private readonly int m_MaxSize;
        private const int DefaultSize = byte.MaxValue;

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
            for (var i = 0; i < Builder.Length; i++) writer.Put(Builder[i]);
        }

        public override void Deserialize(NetDataReader reader)
        {
            int size = reader.GetByte();
            ThrowIfOverMaxSize(size);
            Zero();
            for (var _ = 0; _ < size; _++) Builder.Append(reader.GetChar());
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

        public override void Clear()
        {
            Zero();
            base.Clear();
        }

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
            Builder.AppendProperty(otherString);
            WithValue = true;
        }

        public void SetTo(string @string)
        {
            ThrowIfOverMaxSize(@string.Length);
            Zero();
            Builder.Append(@string);
            WithValue = true;
        }

        public StringProperty Add(string @string)
        {
            ThrowIfOverMaxSize(Builder.Length + @string.Length);
            Builder.Append(@string);
            WithValue = true;
            return this;
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

    public static class StringExtensions
    {
        // TODO:performance look into copying
        public static StringBuilder AppendProperty(this StringBuilder builder, StringProperty other)
        {
            builder.EnsureCapacity(builder.Length + other.Builder.Length);
            for (var i = 0; i < other.Builder.Length; i++) builder.Append(other.Builder[i]);
            return builder;
        }
    }
}