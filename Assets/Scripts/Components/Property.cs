using System;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Components
{
    public abstract class PropertyBase
    {
        public bool HasValue { get; protected set; }

        public abstract Type ValueType { get; }

        public abstract object GetValue();

        public abstract void SetValue(object value);

        public abstract void SetFromIfPresent(PropertyBase other);

        public abstract void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation);
    }

    public class Property<T> : PropertyBase where T : struct
    {
        [Copy] private T m_Value;

        public T Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                HasValue = true;
            }
        }

        public override Type ValueType => typeof(T);

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
            return other.GetType() == ValueType && m_Value.Equals(other) || other.GetType() == GetType() && Equals((Property<T>) other);
        }

        public static implicit operator Property<T>(T property)
        {
            return new Property<T> {m_Value = property, HasValue = true};
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

        public Property<T> IfPresent(Action<T> action)
        {
            if (HasValue) action(m_Value);
            return this;
        }

        public T OrElse(T @default)
        {
            return HasValue ? m_Value : @default;
        }

        public override string ToString()
        {
            return m_Value.ToString();
        }

        public override object GetValue()
        {
            return m_Value;
        }

        public override void SetValue(object value)
        {
            if (!(value is T newValue))
                throw new ArgumentException("Value is not of the proper type");
            m_Value = newValue;
        }

        public override void SetFromIfPresent(PropertyBase other)
        {
            if (!(other is Property<T> otherProperty))
                throw new ArgumentException("Other property is not of the same type");
            if (otherProperty.HasValue)
                Value = otherProperty.Value;
        }

        public override void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation)
        {
            if (!p1.HasValue || !p2.HasValue) return;
            // @formatter:off
            switch (GetValue())
            {
                case Vector3 _: SetValue(Vector3.Lerp((Vector3) p1.GetValue(), (Vector3) p2.GetValue(), interpolation)); break;
                case float   _: SetValue(  Mathf.Lerp(( float ) p1.GetValue(), ( float ) p2.GetValue(), interpolation)); break;
            }
            // @formatter:on
        }
    }
}