using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Util.Math;
using Voxel;

namespace Compound.Session
{
    [Serializable]
    public class VoxelMapNameProperty : StringProperty
    {
        public VoxelMapNameProperty() : base(16) { }
    }
    
    [Serializable]
    public class ChangedVoxelsProperty : PropertyBase, IEnumerable<(Position3Int, VoxelChangeData)>
    {
        private Dictionary<Position3Int, VoxelChangeData> m_ChangeMap = new Dictionary<Position3Int, VoxelChangeData>();

        public override void Serialize(BinaryWriter writer)
        {
            writer.Write(m_ChangeMap.Count);
            foreach ((Position3Int position, VoxelChangeData change) in this)
            {
                Position3Int.Serialize(position, writer);
                VoxelChangeData.Serialize(writer, change);
            }
        }

        public override void Deserialize(BinaryReader reader)
        {
            Clear();
            for (var i = 0; i < reader.ReadInt32(); i++)
                SetVoxel(Position3Int.Deserialize(reader), VoxelChangeData.Deserialize(reader));
        }

        public override bool Equals(PropertyBase other) => throw new NotImplementedException();

        public override void Clear() => m_ChangeMap.Clear();

        public override void Zero() => m_ChangeMap.Clear();

        public override void SetFromIfPresent(PropertyBase other)
        {
            if (!(other is ChangedVoxelsProperty otherChanged)) throw new ArgumentException("Other was not voxel change map");
            Clear();
            foreach ((Position3Int position, VoxelChangeData changeData) in otherChanged)
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
        }

        public override void InterpolateFromIfPresent(PropertyBase p1, PropertyBase p2, float interpolation) => throw new NotImplementedException();

        public IEnumerator<(Position3Int, VoxelChangeData)> GetEnumerator()
        {
            foreach (KeyValuePair<Position3Int, VoxelChangeData> pair in m_ChangeMap)
                yield return (pair.Key, pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }

    public static class CompoundComponents
    {
        public static readonly SessionElements SessionElements;

        static CompoundComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.elements.Add(typeof(VoxelMapNameProperty));
        }
    }
}