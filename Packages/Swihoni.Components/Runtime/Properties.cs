using System;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Swihoni.Components
{
    [Serializable]
    public class UIntProperty : PropertyBase<uint>
    {
        public UIntProperty(uint value) : base(value) { }

        public UIntProperty() { }

        public override void Serialize(BinaryWriter writer) { writer.Write(Value); }

        public override void Deserialize(BinaryReader reader) { Value = reader.ReadUInt32(); }

        public override bool ValueEquals(PropertyBase<uint> other) { return other.Value == Value; }
    }

    [Serializable]
    public class UShortProperty : PropertyBase<ushort>
    {
        public UShortProperty(ushort value) : base(value) { }

        public UShortProperty() { }

        public override void Serialize(BinaryWriter writer) { writer.Write(Value); }

        public override void Deserialize(BinaryReader reader) { Value = reader.ReadUInt16(); }

        public override bool ValueEquals(PropertyBase<ushort> other) { return other.Value == Value; }
    }

    [Serializable]
    public class FloatProperty : PropertyBase<float>
    {
        public FloatProperty(float value) : base(value) { }

        public FloatProperty() { }

        public override void Serialize(BinaryWriter writer) { writer.Write(Value); }

        public override void Deserialize(BinaryReader reader) { Value = reader.ReadSingle(); }

        public override bool ValueEquals(PropertyBase<float> other)
        {
            // TODO:feature add tolerance
            return Mathf.Abs(other.Value - Value) < 0.1f;
        }

        public override void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation, FieldInfo field = null)
        {
            CheckInterpolatable(p1, p2, out PropertyBase<float> fp1, out PropertyBase<float> fp2);
            float f1 = fp1, f2 = fp2;
            if (field.IsDefined(typeof(Angle)))
                Value = Mathf.LerpAngle(f1, f2, interpolation);
            else if (field.IsDefined(typeof(Cyclic)))
            {
                var attribute = field.GetCustomAttribute<Cyclic>();
                float range = attribute.maximum - attribute.minimum;
                while (f1 > f2)
                    f2 += range;
                Value = attribute.minimum + Mathf.Repeat(Mathf.Lerp(f1, f2, interpolation), range);
            }
            else
                Value = Mathf.Lerp(f1, f2, interpolation);
        }
    }

    [Serializable]
    public class ByteProperty : PropertyBase<byte>
    {
        public ByteProperty(byte value) : base(value) { }

        public ByteProperty() { }

        public override void Serialize(BinaryWriter writer) { writer.Write(Value); }

        public override void Deserialize(BinaryReader reader) { Value = reader.ReadByte(); }

        public override bool ValueEquals(PropertyBase<byte> other) { return other.Value == Value; }
    }

    [Serializable]
    public class VectorProperty : PropertyBase<Vector3>
    {
        public VectorProperty(Vector3 value) : base(value) { }

        public VectorProperty(float x, float y, float z) : base(new Vector3(x, y, z)) { }

        public VectorProperty() { }

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Value.x);
            writer.Write(Value.y);
            writer.Write(Value.z);
        }

        public override void Deserialize(BinaryReader reader) { Value = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle()); }

        public override bool ValueEquals(PropertyBase<Vector3> other)
        {
            return Mathf.Abs(Value.x - other.Value.x) < 0.1f
                && Mathf.Abs(Value.y - other.Value.y) < 0.1f
                && Mathf.Abs(Value.z - other.Value.z) < 0.1f;
        }

        public override void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation, FieldInfo field = null)
        {
            CheckInterpolatable(p1, p2, out PropertyBase<Vector3> v1, out PropertyBase<Vector3> v2);
            Value = Vector3.Lerp(v1, v2, interpolation);
        }
    }
}