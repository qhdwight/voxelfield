using System;
using LiteNetLib.Utils;
using Swihoni.Components;

namespace Voxels
{
    [Serializable]
    public class VoxelChangeProperty : PropertyBase<VoxelChange>
    {
        public VoxelChangeProperty() { }
        public VoxelChangeProperty(in VoxelChange value) : base(value) { }
        public override void SerializeValue(NetDataWriter writer) => VoxelChangeSerializer.Serialize(Value, writer);
        public override void DeserializeValue(NetDataReader reader) => Value = VoxelChangeSerializer.Deserialize(reader);
        public override bool ValueEquals(in VoxelChange value) => Value == value;
    }

    [Serializable, SingleTick]
    public class OrderedVoxelChangesProperty : ListPropertyBase<VoxelChange>
    {
        public string Version { get; set; }

        public OrderedVoxelChangesProperty() : base(int.MaxValue) { }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_List.Count);
            foreach (VoxelChange element in m_List)
                VoxelChangeSerializer.Serialize(element, writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            if (count > m_List.Capacity) m_List.Capacity = count;
            for (var _ = 0; _ < count; _++)
                m_List.Add(VoxelChangeSerializer.Deserialize(reader));
            WithValue = true;
        }

        public override bool Equals(PropertyBase other) => throw new NotImplementedException();
    }
}