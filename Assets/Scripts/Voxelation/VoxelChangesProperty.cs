using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util.Math;

namespace Voxelation
{
    [Serializable, SingleTick]
    public class VoxelChangesProperty : DictPropertyBase<Position3Int, VoxelChange>
    {
        public string Version { get; set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_Map.Count);
            foreach (KeyValuePair<Position3Int, VoxelChange> pair in m_Map)
            {
                Position3Int.Serialize(pair.Key, writer);
                VoxelVersionSerializer.Serialize(pair.Value, writer);
            }
        }

        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            for (var _ = 0; _ < count; _++)
            {
                m_Map.Add(Position3Int.Deserialize(reader),
                          VoxelVersionSerializer.Deserialize(reader, Version));
            }
            WithValue = true;
        }
        
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
}