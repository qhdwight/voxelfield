using System;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util.Math;

namespace Voxel
{
    [Serializable, ClearAfterTick]
    public class ChangedVoxelsProperty : DictionaryPropertyBase<Position3Int, VoxelChangeData>, IVoxelChanges
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_Map.Count);
            foreach ((Position3Int position, VoxelChangeData change) in this)
            {
                Position3Int.Serialize(position, writer);
                VoxelChangeData.Serialize(change, writer);
            }
        }

        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            for (var _ = 0; _ < count; _++)
                m_Map.Add(Position3Int.Deserialize(reader), VoxelChangeData.Deserialize(reader));
            WithValue = true;
        }

        public override void SetTo(PropertyBase other)
        {
            if (!(other is ChangedVoxelsProperty otherChanged)) throw new ArgumentException("Other was not changed voxels");
            Clear();
            AddAllFrom(otherChanged);
            WithValue = true;
        }

        public void AddAllFrom(ChangedVoxelsProperty other)
        {
            foreach ((Position3Int position, VoxelChangeData changeData) in other)
                SetVoxel(position, changeData);
        }

        public void SetVoxel(in Position3Int position, VoxelChangeData change)
        {
            if (m_Map.TryGetValue(position, out VoxelChangeData existingChange))
            {
                existingChange.Merge(change);
                m_Map.Remove(position);
                change = existingChange;
            }
            m_Map.Add(position, change);
            WithValue = true;
        }
    }
}