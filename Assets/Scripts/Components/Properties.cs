using System;
using System.IO;
using UnityEngine;

namespace Components
{
    [Serializable]
    public class UIntProperty : PropertyBase<uint>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadUInt32();
        }

        public override bool Equals(PropertyBase other)
        {
            return other is UIntProperty otherProperty && otherProperty.Value == Value;
        }

        public UIntProperty(uint value) : base(value)
        {
        }
        
        public UIntProperty()
        {
        }
    }
    
    [Serializable]
    public class UShortProperty : PropertyBase<ushort>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadUInt16();
        }

        public override bool Equals(PropertyBase other)
        {
            return other is UShortProperty otherProperty && otherProperty.Value == Value;
        }

        public UShortProperty(ushort value) : base(value)
        {
        }

        public UShortProperty()
        {
        }
    }

    [Serializable]
    public class FloatProperty : PropertyBase<float>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadSingle();
        }

        public override bool Equals(PropertyBase other)
        {
            return other is FloatProperty otherProperty && Mathf.Approximately(otherProperty.Value, Value);
        }

        public FloatProperty(float value) : base(value)
        {
        }
        
        public FloatProperty()
        {
        }

        public override void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation)
        {
            if (!(p1 is FloatProperty f1) || !(p2 is FloatProperty f2))
                throw new ArgumentException("Properties are not the proper type!");
            Value = Mathf.Lerp(f1, f2, interpolation);
        }
    }

    [Serializable]
    public class ByteProperty : PropertyBase<byte>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Value);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = reader.ReadByte();
        }

        public override bool Equals(PropertyBase other)
        {
            return other is ByteProperty otherProperty && otherProperty.Value == Value;
        }

        public ByteProperty(byte value) : base(value)
        {
        }
        
        public ByteProperty()
        {
        }
    }

    [Serializable]
    public class VectorProperty : PropertyBase<Vector3>
    {
        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(Value.x);
            writer.Write(Value.y);
            writer.Write(Value.z);
        }

        public override void Deserialize(BinaryReader reader)
        {
            Value = new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public override bool Equals(PropertyBase other)
        {
            return other is VectorProperty otherProperty && otherProperty.Value == Value;
        }

        public override void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation)
        {
            if (!(p1 is VectorProperty v1) || !(p2 is VectorProperty v2))
                throw new ArgumentException("Properties are not the proper type!");
            Value = Vector3.Lerp(v1, v2, interpolation);
        }

        public VectorProperty(Vector3 value) : base(value)
        {
        }
        
        public VectorProperty(float x, float y, float z) : base(new Vector3(x, y, z))
        {
        }
        
        public VectorProperty()
        {
        }
    }
}