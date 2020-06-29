using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util.Math;

namespace Voxel
{
    [Serializable, Additive]
    public class ChangedVoxelsProperty : PropertyBase, IVoxelChanges, IEnumerable<(Position3Int, VoxelChangeData)>
    {
        private Dictionary<Position3Int, VoxelChangeData> m_ChangeMap = new Dictionary<Position3Int, VoxelChangeData>();

        public int Count => m_ChangeMap.Count;

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_ChangeMap.Count);
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
            for (var i = 0; i < count; i++)
                m_ChangeMap.Add(Position3Int.Deserialize(reader), VoxelChangeData.Deserialize(reader));
            WithValue = true;
        }

        public override bool Equals(PropertyBase other) => throw new NotImplementedException();

        public override void Clear()
        {
            Zero();
            base.Clear();
        }

        public override void Zero() => m_ChangeMap.Clear();

        public override void SetFromIfWith(PropertyBase other)
        {
            if (other.WithValue) SetTo(other);
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
            if (m_ChangeMap.TryGetValue(position, out VoxelChangeData existingChange))
            {
                existingChange.Merge(change);
                m_ChangeMap.Remove(position);
                change = existingChange;
            }
            m_ChangeMap.Add(position, change);
            WithValue = true;
        }

        public override void InterpolateFromIfWith(PropertyBase p1, PropertyBase p2, float interpolation) => throw new Exception("Cannot interpolate changed voxels");

        public IEnumerator<(Position3Int, VoxelChangeData)> GetEnumerator()
        {
            foreach (KeyValuePair<Position3Int, VoxelChangeData> pair in m_ChangeMap)
                yield return (pair.Key, pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => $"Count: {m_ChangeMap.Count}";
    }

}