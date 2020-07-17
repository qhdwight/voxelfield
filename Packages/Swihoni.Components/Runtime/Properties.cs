using System;
using System.Text;
using LiteNetLib.Utils;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;

namespace Swihoni.Components
{
    // Thank you lack of compile time C# meta-programming for all this manual code!

    [Serializable]
    public class ColorProperty : PropertyBase<Color>
    {
        public override bool ValueEquals(in Color value) => value == Value;
        public override void SerializeValue(NetDataWriter writer) => writer.PutColor(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetColor();
    }

    [Serializable]
    public class UIntProperty : PropertyBase<uint>
    {
        public UIntProperty(uint value) : base(value) { }
        public UIntProperty() { }
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetUInt();
        public override bool ValueEquals(in uint value) => value == Value;
        public override void ValueInterpolateFrom(PropertyBase<uint> p1, PropertyBase<uint> p2, float interpolation) => Value = InterpolateUInt(p1.Value, p2.Value, interpolation);
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Value);

        public static uint InterpolateUInt(uint u1, uint u2, float interpolation)
        {
            decimal d1 = u1, d2 = u2, i = (decimal) interpolation;
            return (uint) Math.Round(d1 + (d2 - d1) * i);
        }

        public override bool TryParseValue(string stringValue)
        {
            if (!uint.TryParse(stringValue.Expand(), out uint @uint)) return false;
            Value = @uint;
            return true;
        }
    }

    /// <summary>
    /// Ensures proper interpolation for a time, which is assumed to never decrease.
    /// This solves issues where the second time represents a "jump" by resetting to a lower value.
    /// </summary>
    [Serializable]
    public class ElapsedUsProperty : UIntProperty
    {
        public override void ValueInterpolateFrom(PropertyBase<uint> p1, PropertyBase<uint> p2, float interpolation)
            => Value = p2.Value > p1.Value ? InterpolateUInt(p1.Value, p2.Value, interpolation) : p2.Value;

        public override string ToString() => WithValue ? $"Microseconds: {Value}, Seconds: {Value * TimeConversions.MicrosecondToSecond:F3}" : "No Value";
    }

    [Serializable]
    public class TimeUsProperty : UIntProperty
    {
        public override void ValueInterpolateFrom(PropertyBase<uint> p1, PropertyBase<uint> p2, float interpolation)
            => Value = p2.Value < p1.Value ? InterpolateUInt(p1.Value, p2.Value, interpolation) : p2.Value;

        public void Subtract(uint durationUs)
        {
            if (Value > durationUs) Value -= durationUs;
            else Value = 0u;
        }
    }

    [Serializable]
    public class IntProperty : PropertyBase<int>
    {
        public IntProperty(int value) : base(value) { }
        public IntProperty() { }
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetInt();
        public override bool ValueEquals(in int value) => value == Value;
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Value);

        public override bool TryParseValue(string stringValue)
        {
            if (!int.TryParse(stringValue, out int @int)) return false;
            Value = @int;
            return true;
        }
    }

    [Serializable]
    public class UShortProperty : PropertyBase<ushort>
    {
        public UShortProperty(ushort value) : base(value) { }
        public UShortProperty() { }
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetUShort();
        public override bool ValueEquals(in ushort value) => value == Value;
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Value);

        public override bool TryParseValue(string stringValue)
        {
            if (!ushort.TryParse(stringValue, out ushort @ushort)) return false;
            Value = @ushort;
            return true;
        }
    }

    [Serializable]
    public class ShortProperty : PropertyBase<short>
    {
        public ShortProperty(short value) : base(value) { }
        public ShortProperty() { }
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetShort();
        public override bool ValueEquals(in short value) => value == Value;
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Value);

        public override bool TryParseValue(string stringValue)
        {
            if (!short.TryParse(stringValue, out short @short)) return false;
            Value = @short;
            return true;
        }
    }

    [Serializable]
    public class SByteProperty : PropertyBase<sbyte>
    {
        public SByteProperty(sbyte value) : base(value) { }
        public SByteProperty() { }
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetSByte();
        public override bool ValueEquals(in sbyte value) => value == Value;
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Value);

        public override bool TryParseValue(string stringValue)
        {
            if (!sbyte.TryParse(stringValue, out sbyte @sbyte)) return false;
            Value = @sbyte;
            return true;
        }
    }

    [Serializable]
    public class ByteProperty : PropertyBase<byte>
    {
        public ByteProperty(byte value) : base(value) { }
        public ByteProperty() { }
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetByte();
        public override bool ValueEquals(in byte value) => value == Value;
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Value);
        public override int GetHashCode() => Value.GetHashCode();

        public override bool TryParseValue(string stringValue)
        {
            if (!byte.TryParse(stringValue, out byte @byte)) return false;
            Value = @byte;
            return true;
        }
    }

    [Serializable]
    public class Position3IntProperty : PropertyBase<Position3Int>
    {
        public Position3IntProperty() { }
        public Position3IntProperty(int x, int y, int z) => Value = new Position3Int(x, y, z);
        public override bool ValueEquals(in Position3Int value) => value == Value;
        public override void SerializeValue(NetDataWriter writer) => Position3Int.Serialize(Value, writer);
        public override void DeserializeValue(NetDataReader reader) => Value = Position3Int.Deserialize(reader);
    }

    [Serializable]
    public class IdProperty : ByteProperty
    {
        public IdProperty(byte value) : base(value) { }
        public IdProperty() { }
    }

    [Serializable]
    public class BoolProperty : PropertyBase<bool>
    {
        public BoolProperty() { }
        public BoolProperty(bool value) : base(value) { }
        public override bool ValueEquals(in bool value) => value == Value;
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetBool();
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Value);

        public override bool TryParseValue(string stringValue)
        {
            if (!bool.TryParse(stringValue, out bool @bool)) return false;
            Value = @bool;
            return true;
        }
    }

    [Serializable]
    public class QuaternionProperty : PropertyBase<Quaternion>
    {
        public override bool ValueEquals(in Quaternion value) => value == Value;
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
        public override StringBuilder AppendValue(StringBuilder builder) => builder.Append(Value);

        public override bool ValueEquals(in float value)
        {
            float tolerance = TryAttribute(out ToleranceAttribute attribute) ? attribute.tolerance : DefaultFloatTolerance;
            return CheckWithinTolerance(value, tolerance);
        }

        public bool CheckWithinTolerance(in float other, float tolerance) => Mathf.Abs(other - Value) < tolerance;

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
            if (WithAttribute<AngleAttribute>())
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

        public override bool TryParseValue(string stringValue)
        {
            if (!float.TryParse(stringValue, out float @float)) return false;
            Value = @float;
            return true;
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

        public override bool ValueEquals(in Vector3 value)
        {
            float tolerance = TryAttribute(out ToleranceAttribute toleranceAttribute) ? toleranceAttribute.tolerance : DefaultFloatTolerance;
            return CheckWithinTolerance(value, tolerance);
        }

        public bool CheckWithinTolerance(in Vector3 other, float tolerance)
            => Mathf.Abs(Value.x - other.x) < tolerance
            && Mathf.Abs(Value.y - other.y) < tolerance
            && Mathf.Abs(Value.z - other.z) < tolerance;

        public override void ValueInterpolateFrom(PropertyBase<Vector3> p1, PropertyBase<Vector3> p2, float interpolation)
        {
            bool GreaterThan(in Vector3 v1, in Vector3 v2, float range) => (v2 - v1).sqrMagnitude > range * range;
            bool useSecond = TryAttribute(out InterpolateRangeAttribute interpolateRangeAttribute) && GreaterThan(p1, p2, interpolateRangeAttribute.range);
            Value = useSecond ? p2 : Vector3.Lerp(p1, p2, interpolation);
        }
    }
}