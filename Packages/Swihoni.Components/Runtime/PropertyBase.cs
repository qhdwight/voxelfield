using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Components
{
    [Flags]
    public enum ElementFlags : sbyte
    {
        None = 0,
        WithValue = 1,
        DontSerialize = 2,
        WasSame = 4
    }

    /// <summary>
    /// <see cref="PropertyBase{T}"/>
    /// </summary>
    [Serializable]
    public abstract class PropertyBase : ElementBase
    {
        [SerializeField] private ElementFlags m_Flags = ElementFlags.None;

        public sbyte RawFlags
        {
            get => (sbyte) m_Flags;
            set => m_Flags = (ElementFlags) value;
        }

        public bool WithValue
        {
            get => (m_Flags & ElementFlags.WithValue) == ElementFlags.WithValue;
            protected set
            {
                // TODO:refactor generalized methods for setting flags
                if (value) m_Flags |= ElementFlags.WithValue;
                else m_Flags &= ~ElementFlags.WithValue;
            }
        }

        public bool WasSame
        {
            get => (m_Flags & ElementFlags.WasSame) == ElementFlags.WasSame;
            set
            {
                if (value) m_Flags |= ElementFlags.WasSame;
                else m_Flags &= ~ElementFlags.WasSame;
            }
        }

        public bool WithoutValue => !WithValue;

        public bool DontSerialize
        {
            get => (m_Flags & ElementFlags.DontSerialize) == ElementFlags.DontSerialize;
            protected set
            {
                if (value) m_Flags |= ElementFlags.DontSerialize;
                else m_Flags &= ~ElementFlags.DontSerialize;
            }
        }

        public abstract void Serialize(NetDataWriter writer);
        public abstract void Deserialize(NetDataReader reader);
        public abstract bool Equals(PropertyBase other);
        public abstract void Zero();
        public abstract void SetFromIfWith(PropertyBase other);
        public abstract void SetTo(PropertyBase other);
        public abstract void InterpolateFromIfWith(PropertyBase p1, PropertyBase p2, float interpolation);
        public virtual void Clear() => WithValue = false;
    }

    public class WithoutValueException : Exception
    {
        public WithoutValueException(string message) : base(message) { }
    }

    /// <summary>
    /// Wrapper for holding a value.
    /// This is a class, so it is always passed by reference.
    /// This means that extra care needs to be taken with using properties.
    /// They should only ever belong to one container.
    /// They should never be null. Use the <see cref="PropertyBase.WithValue"/> feature instead.
    /// To set values, use <see cref="SetFromIfWith"/> or <see cref="Value"/>. Clear with <see cref="Clear"/>.
    /// Do not assign one property directly to another, as this replaces the reference instead of copying value!
    /// Equality operators are overriden to compare values instead of pointers.
    /// </summary>
    [Serializable]
    public abstract class PropertyBase<T> : PropertyBase where T : struct
    {
        [CopyField, SerializeField] private T m_Value;

        protected const float DefaultFloatTolerance = 1e-5f;

        /// <summary>
        /// Use only if this property is with a value.
        /// If you are unsure, use <see cref="PropertyBase.WithValue"/> or <see cref="IfWith"/>.
        /// </summary>
        /// <returns>Value wrapped by property.</returns>
        /// <exception cref="WithoutValueException">If without value.</exception>
        public T Value
        {
            get
            {
                if (WithValue)
                    return m_Value;
                throw new WithoutValueException($"No value for: {GetType().Name} attached to field: {Field?.Name ?? "None"}!");
            }
            set
            {
                m_Value = value;
                WithValue = true;
            }
        }

        public T? NullableValue => WithValue ? m_Value : (T?) null;

        protected PropertyBase() { }

        protected PropertyBase(T value) => Value = value;

        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);

        public override bool Equals(object other) => this == (PropertyBase<T>) other;

        public static implicit operator T(PropertyBase<T> property) => property.Value;

        public static bool operator ==(PropertyBase<T> p1, PropertyBase<T> p2) => p1.WithValue && p2.WithValue && p1.ValueEquals(p2)
                                                                               || p1.WithoutValue && p2.WithoutValue;

        public static bool operator !=(PropertyBase<T> p1, PropertyBase<T> p2) => !(p1 == p2);

        public PropertyBase<T> IfWith(Action<T> action)
        {
            if (WithValue) action(m_Value);
            return this;
        }

        public override void Zero() => Value = default;

        public T Else(T @default) => WithValue ? m_Value : @default;

        /// <returns>False if types are different. Equal if both values are the same, or if both do not have values.</returns>
        public sealed override bool Equals(PropertyBase other) => this == (PropertyBase<T>) other;

        /// <summary>Use on two properties that are known to have values.</summary>
        /// <returns>False if types are different. Equal if both values are the same.</returns>
        /// <exception cref="WithoutValueException">Without value on at least one property.</exception>
        public abstract bool ValueEquals(PropertyBase<T> other);

        public override string ToString() => WithValue ? m_Value.ToString() : "No Value";

        /// <exception cref="ArgumentException">If types are different.</exception>
        public override void SetFromIfWith(PropertyBase other)
        {
            if (!(other is PropertyBase<T> otherProperty))
                throw new ArgumentException("Other property is not of the same type");
            if (otherProperty.WithValue)
                Value = otherProperty.m_Value;
        }

        public override void SetTo(PropertyBase other)
        {
            if (!(other is PropertyBase<T> otherProperty))
                throw new ArgumentException("Other property is not of the same type");
            if (otherProperty.WithValue) Value = otherProperty.Value;
            else Clear();
        }

        /// <exception cref="ArgumentException">If types are different.</exception>
        public sealed override void InterpolateFromIfWith(PropertyBase p1, PropertyBase p2, float interpolation)
        {
            if (!(p1 is PropertyBase<T> pt1) || !(p2 is PropertyBase<T> pt2))
                throw new ArgumentException("Properties are not the proper type!");
            if (WithAttribute<TakeSecondForInterpolationAttribute>())
            {
                SetFromIfWith(p2);
                return;
            }
            if (pt1.WithValue && pt2.WithValue) ValueInterpolateFrom(pt1, pt2, interpolation);
        }

        /// <summary>Interpolates into this from two properties that are known to have values.</summary>
        /// <exception cref="WithoutValueException">If <see cref="p1"/> or <see cref="p2"/> is without a value.</exception>
        public virtual void ValueInterpolateFrom(PropertyBase<T> p1, PropertyBase<T> p2, float interpolation) => SetFromIfWith(p2);

        public sealed override void Serialize(NetDataWriter writer)
        {
            if (DontSerialize) return;
            writer.Put(RawFlags);
            if (WithoutValue) return;
            SerializeValue(writer);
        }

        public sealed override void Deserialize(NetDataReader reader)
        {
            if (DontSerialize) return;
            RawFlags = reader.GetSByte();
            if (WithoutValue) return;
            DeserializeValue(reader);
        }

        /// <exception cref="WithoutValueException">If without value.</exception>
        public abstract void SerializeValue(NetDataWriter writer);

        /// <exception cref="WithoutValueException">If without value.</exception>
        public abstract void DeserializeValue(NetDataReader reader);
    }
}