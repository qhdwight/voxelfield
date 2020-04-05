using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Components
{
    [Serializable]
    public abstract class PropertyBase
    {
        [SerializeField] private bool m_HasValue;
        private bool m_IsModified;

        public bool HasValue
        {
            get => m_HasValue;
            protected set => m_HasValue = value;
        }

        public bool IsModified
        {
            get => m_IsModified;
            protected set => m_IsModified = value;
        }

        public abstract void Serialize(BinaryWriter writer);

        public abstract void Deserialize(BinaryReader reader);

        public abstract bool Equals(PropertyBase other);

        public abstract void SetFromIfPresent(PropertyBase other);

        public abstract void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation, FieldInfo field = null);
    }

    [Serializable]
    public abstract class PropertyBase<T> : PropertyBase where T : struct
    {
        [CopyField, SerializeField] private T m_Value;

        public T Value
        {
            get => m_Value;
            set
            {
                m_Value = value;
                HasValue = true;
            }
        }

        public T Modified
        {
            get => m_Value;
            set
            {
                Value = value;
                IsModified = true;
            }
        }

        protected PropertyBase()
        {
        }

        protected PropertyBase(T value)
        {
            Value = value;
        }

        public override int GetHashCode()
        {
            return RuntimeHelpers.GetHashCode(this);
        }

        public override bool Equals(object other)
        {
            if (other is null) throw new ArgumentException("Second in equality comparison was null");
            if (ReferenceEquals(this, other)) return true;
            return other.GetType() == GetType() && Equals((PropertyBase<T>) other);
        }

        public static implicit operator T(PropertyBase<T> property)
        {
            return property.m_Value;
        }

        public static bool operator ==(PropertyBase<T> p1, PropertyBase<T> p2)
        {
            if (p1 is null)
                throw new ArgumentException("First in equality comparison was null");
            return p1.Equals(p2);
        }

        public static bool operator !=(PropertyBase<T> p1, PropertyBase<T> p2)
        {
            return !(p1 == p2);
        }

        public PropertyBase<T> IfPresent(Action<T> action)
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

        public override void SetFromIfPresent(PropertyBase other)
        {
            if (!(other is PropertyBase<T> otherProperty))
                throw new ArgumentException("Other property is not of the same type");
            if (otherProperty.HasValue)
                Value = otherProperty.m_Value;
        }

        protected void CheckInterpolatable(PropertyBase p1, PropertyBase p2, out PropertyBase<T> op1, out PropertyBase<T> op2)
        {
            if (!(p1 is PropertyBase<T>) || !(p2 is PropertyBase<T>))
                throw new ArgumentException("Properties are not the proper type!");
            op1 = (PropertyBase<T>) p1;
            op2 = (PropertyBase<T>) p2;
        }

        public override void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation, FieldInfo field = null)
        {
            SetFromIfPresent(p2);
        }
    }
}