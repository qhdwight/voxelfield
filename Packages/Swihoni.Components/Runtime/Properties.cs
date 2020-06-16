using System;
using System.Reflection;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Components
{
    // Thank you lack of compile time C# meta-programming for all this manual code!

    [Serializable]
    public class UIntProperty : PropertyBase<uint>
    {
        public UIntProperty(uint value) : base(value) { }
        public UIntProperty() { }

        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetUInt();
        public override bool ValueEquals(PropertyBase<uint> other) => other.Value == Value;
        public override void ValueInterpolateFrom(PropertyBase<uint> p1, PropertyBase<uint> p2, float interpolation) => Value = InterpolateUInt(p1.Value, p2.Value, interpolation);

        // public static uint InterpolateUInt(uint u1, uint u2, float interpolation) => checked((uint) Math.Round(u1 + (u2 - u1) * interpolation));
        public static uint InterpolateUInt(uint u1, uint u2, float interpolation)
        {
            decimal d1 = u1, d2 = u2, i = (decimal) interpolation;
            return (uint) Math.Round(d1 + (d2 - d1) * i);
        }
    }

    [Serializable]
    public class IntProperty : PropertyBase<int>
    {
        public IntProperty(int value) : base(value) { }
        public IntProperty() { }

        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetInt();
        public override bool ValueEquals(PropertyBase<int> other) => other.Value == Value;
    }

    [Serializable]
    public class UShortProperty : PropertyBase<ushort>
    {
        public UShortProperty(ushort value) : base(value) { }
        public UShortProperty() { }

        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetUShort();
        public override bool ValueEquals(PropertyBase<ushort> other) => other.Value == Value;
    }

    [Serializable]
    public class ShortProperty : PropertyBase<short>
    {
        public ShortProperty(short value) : base(value) { }
        public ShortProperty() { }

        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetShort();
        public override bool ValueEquals(PropertyBase<short> other) => other.Value == Value;
    }

    [Serializable]
    public class SByteProperty : PropertyBase<sbyte>
    {
        public SByteProperty(sbyte value) : base(value) { }
        public SByteProperty() { }

        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetSByte();
        public override bool ValueEquals(PropertyBase<sbyte> other) => other.Value == Value;
    }

    [Serializable]
    public class ByteProperty : PropertyBase<byte>
    {
        public ByteProperty(byte value) : base(value) { }
        public ByteProperty() { }

        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetByte();
        public override bool ValueEquals(PropertyBase<byte> other) => other.Value == Value;
    }

    [Serializable]
    public class BoolProperty : PropertyBase<bool>
    {
        public override bool ValueEquals(PropertyBase<bool> other) => other.Value == Value;
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetBool();
    }

    [Serializable]
    public class QuaternionProperty : PropertyBase<Quaternion>
    {
        public override bool ValueEquals(PropertyBase<Quaternion> other) => other.Value == Value;
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void Zero() => Value = Quaternion.identity;
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetQuaternion();

        public override void ValueInterpolateFrom(PropertyBase<Quaternion> p1, PropertyBase<Quaternion> p2, float interpolation) =>
            Value = Quaternion.Lerp(p1.Value, p2.Value, interpolation);
    }

    [Serializable]
    public class FloatProperty : PropertyBase<float>
    {
        public FloatProperty(float value) : base(value) { }
        public FloatProperty() { }

        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetFloat();

        public override bool ValueEquals(PropertyBase<float> other)
        {
            float tolerance = TryAttribute(out ToleranceAttribute attribute) ? attribute.tolerance : DefaultFloatTolerance;
            return CheckWithinTolerance(other, tolerance);
        }

        public bool CheckWithinTolerance(PropertyBase<float> other, float tolerance) => Mathf.Abs(other.Value - Value) < tolerance;

        public void CyclicInterpolateFrom(float f1, float f2, float min, float max, float interpolation)
        {
            float range = max - min;
            while (f1 > f2) // TODO:performance non-naive method
                f2 += range;
            Value = min + Mathf.Repeat(Mathf.Lerp(f1, f2, interpolation), range);
        }

        public override void ValueInterpolateFrom(PropertyBase<float> p1, PropertyBase<float> p2, float interpolation)
        {
            float f1 = p1, f2 = p2;
            if (HasAttribute<AngleAttribute>())
            {
                Value = Mathf.LerpAngle(f1, f2, interpolation);
                return;
            }
            if (TryAttribute(out CyclicAttribute cyclicAttribute))
            {
                CyclicInterpolateFrom(f1, f2, cyclicAttribute.minimum, cyclicAttribute.maximum, interpolation);
                return;
            }
            if (float.IsPositiveInfinity(f1) || float.IsPositiveInfinity(f2)) Value = float.PositiveInfinity;
            else Value = Mathf.Lerp(f1, f2, interpolation);
        }
    }

    [Serializable]
    public class VectorProperty : PropertyBase<Vector3>
    {
        public VectorProperty(Vector3 value) : base(value) { }
        public VectorProperty(float x, float y, float z) : base(new Vector3(x, y, z)) { }
        public VectorProperty() { }

        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetVector3();

        public override bool ValueEquals(PropertyBase<Vector3> other)
        {
            float tolerance = TryAttribute(out ToleranceAttribute toleranceAttribute) ? toleranceAttribute.tolerance : DefaultFloatTolerance;
            return CheckWithinTolerance(other, tolerance);
        }

        public bool CheckWithinTolerance(PropertyBase<Vector3> other, float tolerance)
            => Mathf.Abs(Value.x - other.Value.x) < tolerance
            && Mathf.Abs(Value.y - other.Value.y) < tolerance
            && Mathf.Abs(Value.z - other.Value.z) < tolerance;

        public override void ValueInterpolateFrom(PropertyBase<Vector3> p1, PropertyBase<Vector3> p2, float interpolation)
        {
            bool GreaterThan(in Vector3 v1, in Vector3 v2, float range) => (v2 - v1).sqrMagnitude > range * range;
            bool useSecond = TryAttribute(out InterpolateRangeAttribute interpolateRangeAttribute) && GreaterThan(p1, p2, interpolateRangeAttribute.range);
            Value = useSecond ? p2 : Vector3.Lerp(p1, p2, interpolation);
        }
    }
}