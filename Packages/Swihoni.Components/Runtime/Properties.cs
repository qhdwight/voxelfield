using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Swihoni.Components
{
    // Thank you lack of compile time C# meta-programming for all this manual code!

    [Serializable]
    public class UIntProperty : PropertyBase<uint>
    {
        public UIntProperty(uint value) : base(value) { }
        public UIntProperty() { }

        public override void SerializeValue(BinaryWriter writer) => writer.Write(Value);
        public override void DeserializeValue(BinaryReader reader) => Value = reader.ReadUInt32();
        public override bool ValueEquals(PropertyBase<uint> other) => other.Value == Value;
    }

    [Serializable]
    public class UShortProperty : PropertyBase<ushort>
    {
        public UShortProperty(ushort value) : base(value) { }
        public UShortProperty() { }

        public override void SerializeValue(BinaryWriter writer) => writer.Write(Value);
        public override void DeserializeValue(BinaryReader reader) => Value = reader.ReadUInt16();
        public override bool ValueEquals(PropertyBase<ushort> other) => other.Value == Value;
    }

    [Serializable]
    public class BoolProperty : PropertyBase<bool>
    {
        public override bool ValueEquals(PropertyBase<bool> other) => other.Value == Value;
        public override void SerializeValue(BinaryWriter writer) => writer.Write(Value);
        public override void DeserializeValue(BinaryReader reader) => Value = reader.ReadBoolean();
    }

    [Serializable]
    public class QuaternionProperty : PropertyBase<Quaternion>
    {
        public override bool ValueEquals(PropertyBase<Quaternion> other) => other.Value == Value;

        public override void SerializeValue(BinaryWriter writer) => writer.Write(Value);

        public override void Zero() => Value = Quaternion.identity;

        public override void DeserializeValue(BinaryReader reader) => Value = reader.ReadQuaternion();

        public override void ValueInterpolateFrom(PropertyBase<Quaternion> p1, PropertyBase<Quaternion> p2, float interpolation) =>
            Value = Quaternion.Lerp(p1.Value, p2.Value, interpolation);
    }

    [Serializable]
    public class FloatProperty : PropertyBase<float>
    {
        public FloatProperty(float value) : base(value) { }
        public FloatProperty() { }

        public override void SerializeValue(BinaryWriter writer) => writer.Write(Value);
        public override void DeserializeValue(BinaryReader reader) => Value = reader.ReadSingle();

        public override bool ValueEquals(PropertyBase<float> other)
        {
            float tolerance;
            if (Field != null && Field.IsDefined(typeof(Tolerance))) tolerance = Field.GetCustomAttribute<Tolerance>().tolerance;
            else tolerance = DefaultFloatTolerance;
            return Mathf.Abs(other.Value - Value) < tolerance;
        }

        public void CyclicInterpolateFrom(float f1, float f2, float min, float max, float interpolation)
        {
            float range = max - min;
            while (f1 > f2)
                f2 += range;
            Value = min + Mathf.Repeat(Mathf.Lerp(f1, f2, interpolation), range);
        }

        public override void ValueInterpolateFrom(PropertyBase<float> p1, PropertyBase<float> p2, float interpolation)
        {
            float f1 = p1, f2 = p2;
            if (Field != null)
            {
                if (Field.IsDefined(typeof(Angle)))
                {
                    Value = Mathf.LerpAngle(f1, f2, interpolation);
                    return;
                }
                if (Field.IsDefined(typeof(Cyclic)))
                {
                    var cyclicAttribute = Field.GetCustomAttribute<Cyclic>();
                    CyclicInterpolateFrom(f1, f2, cyclicAttribute.minimum, cyclicAttribute.maximum, interpolation);
                    return;
                }
            }
            Value = Mathf.Lerp(f1, f2, interpolation);
        }
    }

    [Serializable]
    public class ByteProperty : PropertyBase<byte>
    {
        public ByteProperty(byte value) : base(value) { }
        public ByteProperty() { }

        public override void SerializeValue(BinaryWriter writer) => writer.Write(Value);
        public override void DeserializeValue(BinaryReader reader) => Value = reader.ReadByte();
        public override bool ValueEquals(PropertyBase<byte> other) => other.Value == Value;
    }

    [Serializable]
    public class VectorProperty : PropertyBase<Vector3>
    {
        public VectorProperty(Vector3 value) : base(value) { }
        public VectorProperty(float x, float y, float z) : base(new Vector3(x, y, z)) { }
        public VectorProperty() { }

        public override void SerializeValue(BinaryWriter writer) => writer.Write(Value);
        public override void DeserializeValue(BinaryReader reader) => Value = reader.ReadVector3();

        public override bool ValueEquals(PropertyBase<Vector3> other)
        {
            float tolerance;
            if (Field != null && Field.IsDefined(typeof(Tolerance))) tolerance = Field.GetCustomAttribute<Tolerance>().tolerance;
            else tolerance = DefaultFloatTolerance;
            return Mathf.Abs(Value.x - other.Value.x) < tolerance
                && Mathf.Abs(Value.y - other.Value.y) < tolerance
                && Mathf.Abs(Value.z - other.Value.z) < tolerance;
        }

        public override void ValueInterpolateFrom(PropertyBase<Vector3> p1, PropertyBase<Vector3> p2, float interpolation) => Value = Vector3.Lerp(p1, p2, interpolation);
    }
}