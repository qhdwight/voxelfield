using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Components
{
    public abstract class PropertyBase
    {
    }

    [DebuggerDisplay("{m_Value}")]
    public class Property<T> : PropertyBase where T : struct
    {
        [Copy] protected T m_Value;

        public T Value => m_Value;

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        protected bool Equals(Property<T> other)
        {
            return m_Value.Equals(other.m_Value);
        }

        public override bool Equals(object other)
        {
            if (other is null) throw new ArgumentException("Other in equality comparison was null");
            if (ReferenceEquals(this, other)) return true;
            return other.GetType() == GetType() && Equals((Property<T>) other);
        }

        public static implicit operator Property<T>(T property)
        {
            return new Property<T> {m_Value = property};
        }

        public static implicit operator T(Property<T> property)
        {
            return property.m_Value;
        }

        public static bool operator ==(Property<T> p1, Property<T> p2)
        {
            if (p1 is null || p2 is null) throw new ArgumentException("A property in equality comparison was null");
            return p1.m_Value.Equals(p2.m_Value);
        }

        public static bool operator !=(Property<T> p1, Property<T> p2)
        {
            return !(p1 == p2);
        }
    }
}