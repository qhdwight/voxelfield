using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Swihoni.Components
{
    [Serializable]
    public class UIntProperty : PropertyBase<uint>
    {
        public UIntProperty(uint value) : base(value)
        {
        }

        public UIntProperty()
        {
        }

        public override void SerializeValue(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public override void DeserializeValue(BinaryReader reader)
        {
            Value = reader.ReadUInt32();
        }

        public override bool ValueEquals(PropertyBase<uint> other)
        {
            return other.Value == Value;
        }
    }

    [Serializable]
    public class UShortProperty : PropertyBase<ushort>
    {
        public UShortProperty(ushort value) : base(value)
        {
        }

        public UShortProperty()
        {
        }

        public override void SerializeValue(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public override void DeserializeValue(BinaryReader reader)
        {
            Value = reader.ReadUInt16();
        }

        public override bool ValueEquals(PropertyBase<ushort> other)
        {
            return other.Value == Value;
        }
    }

    [Serializable]
    public class FloatProperty : PropertyBase<float>
    {
        public FloatProperty(float value) : base(value)
        {
        }

        public FloatProperty()
        {
        }

        public override void SerializeValue(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public override void DeserializeValue(BinaryReader reader)
        {
            Value = reader.ReadSingle();
        }

        public override bool ValueEquals(PropertyBase<float> other)
        {
            float tolerance;
            if (Field != null && Field.IsDefined(typeof(Tolerance))) tolerance = Field.GetCustomAttribute<Tolerance>().tolerance;
            else tolerance = DefaultFloatTolerance;
            return Mathf.Abs(other.Value - Value) < tolerance;
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
                    float range = cyclicAttribute.maximum - cyclicAttribute.minimum;
                    while (f1 > f2)
                        f2 += range;
                    Value = cyclicAttribute.minimum + Mathf.Repeat(Mathf.Lerp(f1, f2, interpolation), range);
                    return;
                }
            }
            Value = Mathf.Lerp(f1, f2, interpolation);
        }
    }

    [Serializable]
    public class ByteProperty : PropertyBase<byte>
    {
        public ByteProperty(byte value) : base(value)
        {
        }

        public ByteProperty()
        {
        }

        public override void SerializeValue(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public override void DeserializeValue(BinaryReader reader)
        {
            Value = reader.ReadByte();
        }

        public override bool ValueEquals(PropertyBase<byte> other)
        {
            return other.Value == Value;
        }
    }

    [Serializable]
    public class VectorProperty : PropertyBase<Vector3>
    {
        public VectorProperty(Vector3 value) : base(value)
        {
        }

        public VectorProperty(float x, float y, float z) : base(new Vector3(x, y, z))
        {
        }

        public VectorProperty()
        {
        }

        public override void SerializeValue(BinaryWriter writer)
        {
            writer.Write(Value.x);
            writer.Write(Value.y);
            writer.Write(Value.z);
        }

        public override void DeserializeValue(BinaryReader reader)
        {
            Value = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public override bool ValueEquals(PropertyBase<Vector3> other)
        {
            float tolerance;
            if (Field != null && Field.IsDefined(typeof(Tolerance))) tolerance = Field.GetCustomAttribute<Tolerance>().tolerance;
            else tolerance = DefaultFloatTolerance;
            return Mathf.Abs(Value.x - other.Value.x) < tolerance
                && Mathf.Abs(Value.y - other.Value.y) < tolerance
                && Mathf.Abs(Value.z - other.Value.z) < tolerance;
        }

        public override void ValueInterpolateFrom(PropertyBase<Vector3> p1, PropertyBase<Vector3> p2, float interpolation)
        {
            Value = Vector3.Lerp(p1, p2, interpolation);
        }
    }
}