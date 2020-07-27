using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util.Math;

namespace Voxels
{
    [Serializable]
    public class VoxelChangeProperty : PropertyBase<VoxelChange>
    {
        public VoxelChangeProperty() { }
        public VoxelChangeProperty(VoxelChange value) : base(value) { }
        public override void SerializeValue(NetDataWriter writer) => VoxelChangeSerializer.Serialize(Value, writer);
        public override void DeserializeValue(NetDataReader reader) => Value = VoxelChangeSerializer.Deserialize(reader);
    }

    [Serializable]
    public class VoxelChangesProperty : DictPropertyBase<Position3Int, VoxelChange>
    {
        public string Version { get; set; }

        public override void Serialize(NetDataWriter writer) => throw new NotSupportedException();
        public override void Deserialize(NetDataReader reader) => throw new NotSupportedException();

        public override void Set(in Position3Int position, in VoxelChange change)
        {
            VoxelChange final = change;
            if (m_Map.TryGetValue(position, out VoxelChange existingChange))
            {
                existingChange.Merge(final);
                m_Map.Remove(position);
                final = existingChange;
            }
            m_Map.Add(position, final);
            WithValue = true;
        }
    }

    [Serializable, SingleTick]
    public class OrderedVoxelChangesProperty : VoxelChangesProperty
    {
        public List<Position3Int> OrderedKeys { get; } = new List<Position3Int>();

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(OrderedKeys.Count);
            foreach (Position3Int position in OrderedKeys)
            {
                Position3Int.Serialize(position, writer);
                VoxelChangeSerializer.Serialize(m_Map[position], writer);
            }
        }
        
        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            OrderedKeys.Capacity = count;
            for (var _ = 0; _ < count; _++)
            {
                Position3Int position = Position3Int.Deserialize(reader);
                OrderedKeys.Add(position);
                m_Map.Add(position, VoxelChangeSerializer.Deserialize(reader, Version));
            }
            WithValue = true;
        }

        public override void Zero()
        {
            base.Zero();
            OrderedKeys.Clear();
        }

        public override void Set(in Position3Int position, in VoxelChange change)
        {
            VoxelChange final = change;
            if (m_Map.TryGetValue(position, out VoxelChange existingChange))
            {
                existingChange.Merge(final);
                m_Map.Remove(position);
                final = existingChange;
                // TODO:performance order N
                OrderedKeys.Remove(position);
            }
            m_Map.Add(position, final);
            OrderedKeys.Add(position);
            WithValue = true;
        }
    }
}