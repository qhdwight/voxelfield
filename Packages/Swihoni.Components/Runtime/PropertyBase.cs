using System;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using UnityEngine;

namespace Swihoni.Components
{
    [Serializable]
    public abstract class PropertyBase : ElementBase
    {
        [SerializeField] private bool m_HasValue;
        private bool m_DoSerialize = true;

        public FieldInfo Field { get; set; }

        public bool HasValue
        {
            get => m_HasValue;
            protected set => m_HasValue = value;
        }

        public bool DoSerialization
        {
            get => m_DoSerialize;
            set => m_DoSerialize = value;
        }

        public abstract void Serialize(BinaryWriter writer);

        public abstract void Deserialize(BinaryReader reader);

        public abstract bool Equals(PropertyBase other);

        public abstract void Clear();

        public abstract void Zero();

        public abstract void SetFromIfPresent(PropertyBase other);

        public abstract void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation);
    }

    [Serializable]
    public abstract class PropertyBase<T> : PropertyBase where T : struct
    {
        [CopyField, SerializeField] private T m_Value;

        protected const float DefaultFloatTolerance = 0.1f;

        public T Value
        {
            get
            {
                if (HasValue)
                    return m_Value;
                throw new Exception($"No value for: {GetType().Name} attached to field: {Field?.Name ?? "None"}!");
            }
            set
            {
                m_Value = value;
                HasValue = true;
            }
        }

        public T? NullableValue => HasValue ? m_Value : (T?) null;

        protected PropertyBase() { }

        protected PropertyBase(T value) => Value = value;

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        public override bool Equals(object other)
        {
            if (other is null) throw new ArgumentException("Second in equality comparison was null");
            if (ReferenceEquals(this, other)) return true;
            var otherProperty = (PropertyBase) other;
            return Equals(otherProperty);
        }

        public static implicit operator T(PropertyBase<T> property) => property.Value;

        public static bool operator ==(PropertyBase<T> p1, PropertyBase<T> p2)
        {
            if (p1 is null)
                throw new ArgumentException("First in equality comparison was null");
            return p1.Equals(p2);
        }

        public static bool operator !=(PropertyBase<T> p1, PropertyBase<T> p2) => !(p1 == p2);

        public PropertyBase<T> IfPresent(Action<T> action)
        {
            if (HasValue) action(m_Value);
            return this;
        }

        public override void Clear()
        {
            HasValue = false;
            m_Value = default;
        }

        public override void Zero() => Value = default;

        public T OrElse(T @default) => HasValue ? m_Value : @default;

        public sealed override bool Equals(PropertyBase other)
        {
            return other.GetType() == GetType()
                && HasValue && other.HasValue && ValueEquals((PropertyBase<T>) other)
                || !HasValue && !other.HasValue;
        }

        public abstract bool ValueEquals(PropertyBase<T> other);

        public override string ToString() => HasValue ? m_Value.ToString() : "No Value";

        public override void SetFromIfPresent(PropertyBase other)
        {
            if (!(other is PropertyBase<T> otherProperty))
                throw new ArgumentException("Other property is not of the same type");
            if (otherProperty.HasValue)
                Value = otherProperty.m_Value;
        }

        public sealed override void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation)
        {
            if (!(p1 is PropertyBase<T>) || !(p2 is PropertyBase<T>))
                throw new ArgumentException("Properties are not the proper type!");
            if (Field != null)
            {
                if (Field.IsDefined(typeof(CustomInterpolation))) return;
                if (Field.IsDefined(typeof(TakeSecondForInterpolation)))
                {
                    SetFromIfPresent(p2);
                    return;
                }
            }
            ValueInterpolateFrom((PropertyBase<T>) p1, (PropertyBase<T>) p2, interpolation);
        }

        public virtual void ValueInterpolateFrom(PropertyBase<T> p1, PropertyBase<T> p2, float interpolation) => SetFromIfPresent(p2);

        public sealed override void Serialize(BinaryWriter writer)
        {
            if (!DoSerialization || Field != null && Field.IsDefined(typeof(NoSerialization))) return;
            writer.Write(HasValue);
            if (!HasValue) return;
            SerializeValue(writer);
        }

        public sealed override void Deserialize(BinaryReader reader)
        {
            if (!DoSerialization || Field != null && Field.IsDefined(typeof(NoSerialization))) return;
            HasValue = reader.ReadBoolean();
            if (!HasValue) return;
            DeserializeValue(reader);
        }

        public abstract void SerializeValue(BinaryWriter writer);

        public abstract void DeserializeValue(BinaryReader reader);
    }
}