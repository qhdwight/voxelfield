using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Text;
using LiteNetLib.Utils;
using Swihoni.Collections;
using UnityEngine;

namespace Swihoni.Components
{
    [Flags]
    public enum ElementFlags : byte
    {
        None = 0,
        WithValue = 1,
        DontSerialize = 2,
        WasSame = 4,
        IsOverride = 8
    }

    /// <summary>
    /// <see cref="PropertyBase{T}"/>
    /// </summary>
    [Serializable]
    public abstract class PropertyBase : ElementBase
    {
        [SerializeField] private ElementFlags m_Flags = ElementFlags.None;

        public byte RawFlags
        {
            get => (byte) m_Flags;
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

        public bool IsOverride
        {
            get => (m_Flags & ElementFlags.IsOverride) == ElementFlags.IsOverride;
            set
            {
                if (value) m_Flags |= ElementFlags.IsOverride;
                else m_Flags &= ~ElementFlags.IsOverride;
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

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ (int) m_Flags;
            }
        }

        public abstract void Serialize(NetDataWriter writer);
        public abstract void Deserialize(NetDataReader reader);
        public abstract bool Equals(PropertyBase other);
        public abstract void Zero();
        public abstract void SetTo(PropertyBase other);

        public virtual void InterpolateFrom(PropertyBase p1, PropertyBase p2, float interpolation)
            => throw new NotSupportedException($"Interpolating this property is not supported. Override {GetType().Name}.{nameof(InterpolateFrom)} if this is not intentional.");

        public void SetToIncludingOverride(PropertyBase other)
        {
            SetTo(other);
            other.IsOverride = IsOverride;
        }

        public virtual StringBuilder AppendValue(StringBuilder builder)
            => throw new NotSupportedException($"Appending this property is not supported. Override {GetType().Name}.{nameof(AppendValue)} if this is not intentional.");

        public virtual void ParseValue(string stringValue)
            => throw new NotSupportedException($"Parsing this property is not supported. Override {GetType().Name}.{nameof(ParseValue)} if this is not intentional.");

        public virtual bool TryParseValue(string propertyString)
        {
            try
            {
                ParseValue(propertyString);
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public virtual void Clear() => WithValue = false;

        public virtual void SetFromIfWith(PropertyBase other)
        {
            if (other.WithValue) SetTo(other);
        }
    }

    public class WithoutValueException : Exception
    {
        public WithoutValueException(string message) : base(message) { }
    }

    /**
     * Serialization and equality testing require boxing which allocates, so only suitable for contexts where this does not matter.
     * If performance is need, explicitly create a type deriving from <see cref="PropertyBase{T}"/>.
     */
    [Serializable]
    public class BoxedEnumProperty<TEnum> : PropertyBase<TEnum> where TEnum : struct, Enum
    {
        public static DualDictionary<TEnum, string> Names { get; } = typeof(TEnum).GetNameMap<TEnum>();
        public static DualDictionary<TEnum, string> DisplayNames { get; } = typeof(TEnum).GetNameMap<TEnum>(s => s.Replace("Alpha", string.Empty).ToDisplayCase());

        public BoxedEnumProperty() { }
        public BoxedEnumProperty(TEnum value) : base(value) { }
        public override bool ValueEquals(in TEnum value) => Equals(Value, value);
        public override void SerializeValue(NetDataWriter writer) => writer.Put((int) (object) Value);
        public override void DeserializeValue(NetDataReader reader) => Value = (TEnum) (object) reader.GetInt();
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Names.GetForward(Value));
        public override void ParseValue(string stringValue) => Value = Names.GetReverse(stringValue);
    }

    /// <summary>
    /// Wrapper for holding a value.
    /// This is a class, so it is always passed by reference.
    /// This means that extra care needs to be taken with using properties.
    /// They should only ever belong to one container.
    /// They should never be null. Use the <see cref="PropertyBase.WithValue"/> feature instead.
    /// To set values, use <see cref="SetFromIfWith"/> or <see cref="Value"/>. Clear with <see cref="PropertyBase.Clear"/>.
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
        /// If you are unsure, use <see cref="PropertyBase.WithValue"/> or <see cref="TryWithValue"/>.
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

        public T ValueOverride
        {
            get => Value;
            set
            {
                Value = value;
                IsOverride = true;
            }
        }

        public ref T DirectValue => ref m_Value;

        public T? AsNullable => WithValue ? m_Value : (T?) null;

        protected PropertyBase() { }

        protected PropertyBase(T value) => Value = value;

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ m_Value.GetHashCode();
            }
        }

        public override bool Equals(object other) => this == (PropertyBase<T>) other;

        public static implicit operator T(PropertyBase<T> property) => property.Value;

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        public static bool operator ==(PropertyBase<T> p1, PropertyBase<T> p2) => p1.WithValue && p2.WithValue && p1.ValueEquals(p2)
                                                                               || p1.WithoutValue && p2.WithoutValue;

        public static bool operator !=(PropertyBase<T> p1, PropertyBase<T> p2) => !(p1 == p2);

        public bool SetValueIfWithout(T value = default)
        {
            if (WithValue) return false;
            Value = value;
            return false;
        }

        public bool CompareUpdate(PropertyBase<T> other)
        {
            if (this == other) return false;
            other.SetTo(this);
            return true;
        }

        public bool TryWithValue(out T value)
        {
            if (WithValue)
            {
                value = Value;
                return true;
            }
            value = default;
            return false;
        }

        public PropertyBase<T> If(Action<T> action)
        {
            if (WithValue) action(m_Value);
            return this;
        }

        public override void Zero() => Value = default;

        public T Else(T @default = default) => WithValue ? m_Value : @default;

        /// <returns>False if types are different. Equal if both values are the same, or if both do not have values.</returns>
        public sealed override bool Equals(PropertyBase other) => this == (PropertyBase<T>) other;

        /// <summary>Use on two properties that are known to have values.</summary>
        /// <returns>False if types are different. Equal if both values are the same.</returns>
        /// <exception cref="WithoutValueException">Without value on at least one property.</exception>
        public virtual bool ValueEquals(in T value) => throw new NotImplementedException();

        public override string ToString() => $"[{Field?.Name ?? "No field"}] ({GetType().Name}), {(WithValue ? m_Value.ToString() : "No Value")}";

        public bool WithValueEqualTo(in T value) => WithValue && ValueEquals(value);

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
            SetTo(otherProperty);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void SetTo(PropertyBase<T> other)
        {
            if (other.WithValue) Value = other.Value;
            else Clear();
        }

        public void SetToNullable(T? nullable)
        {
            if (nullable is T value) Value = value;
            else Clear();
        }

        /// <exception cref="ArgumentException">If types are different.</exception>
        public sealed override void InterpolateFrom(PropertyBase p1, PropertyBase p2, float interpolation)
        {
            if (!(p1 is PropertyBase<T> pt1) || !(p2 is PropertyBase<T> pt2))
                throw new ArgumentException("Properties are not the proper type!");
            if (pt1.WithoutValue || pt2.WithoutValue || WithAttribute<TakeSecondForInterpolationAttribute>())
            {
                SetTo(p2);
                return;
            }
            ValueInterpolateFrom(pt1, pt2, interpolation);
        }

        /// <summary>Interpolates into this from two properties that are known to have values.</summary>
        /// <exception cref="WithoutValueException">If <see cref="p1"/> or <see cref="p2"/> is without a value.</exception>
        public virtual void ValueInterpolateFrom(PropertyBase<T> p1, PropertyBase<T> p2, float interpolation) => SetFromIfWith(p2);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override void Serialize(NetDataWriter writer)
        {
            if (DontSerialize) return;
            writer.Put(RawFlags);
            if (WithoutValue) return;
            SerializeValue(writer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public sealed override void Deserialize(NetDataReader reader)
        {
            if (DontSerialize) return;
            RawFlags = reader.GetByte();
            if (WithValue) DeserializeValue(reader);
            else Clear();
        }

        /// <exception cref="WithoutValueException">If without value.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void SerializeValue(NetDataWriter writer)
            => throw new NotSupportedException($"Serializing this property is not supported. Override {GetType().Name}.{nameof(SerializeValue)} if this is not intentional");

        /// <exception cref="WithoutValueException">If without value.</exception>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public virtual void DeserializeValue(NetDataReader reader)
            => throw new NotSupportedException($"Deserializing this property is not supported. Override {GetType().Name}.{nameof(DeserializeValue)} if this is not intentional");
    }
}