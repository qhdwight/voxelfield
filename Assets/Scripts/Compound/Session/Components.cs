using System;
using System.Collections;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Util.Math;
using Voxel;

namespace Compound.Session
{
    [Serializable]
    public class VoxelMapNameElement : StringElement
    {
        public VoxelMapNameElement() : base(16) { }
    }

    [Serializable, Additive]
    public class ChangedVoxelsProperty : PropertyBase, IVoxelChanges, IEnumerable<(Position3Int, VoxelChangeData)>
    {
        private Dictionary<Position3Int, VoxelChangeData> m_ChangeMap = new Dictionary<Position3Int, VoxelChangeData>();

        public int Count => m_ChangeMap.Count;

        public bool IsMaster { get; set; }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_ChangeMap.Count);
            foreach ((Position3Int position, VoxelChangeData change) in this)
            {
                Position3Int.Serialize(position, writer);
                VoxelChangeData.Serialize(writer, change);
            }
        }

        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            for (var i = 0; i < count; i++)
                m_ChangeMap.Add(Position3Int.Deserialize(reader), VoxelChangeData.Deserialize(reader));
        }

        public override bool Equals(PropertyBase other) => throw new NotImplementedException();
        
        public override void Clear() => m_ChangeMap.Clear();
        public override void Zero() => m_ChangeMap.Clear();
        public override void SetFromIfWith(PropertyBase other) => SetTo(other);

        public override void SetTo(PropertyBase other)
        {
            if (!(other is ChangedVoxelsProperty otherChanged)) throw new ArgumentException("Other was not voxel change map");
            Clear();
            AddAllFrom(otherChanged);
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
        }

        public override void InterpolateFromIfWith(PropertyBase p1, PropertyBase p2, float interpolation) => throw new NotImplementedException();

        public IEnumerator<(Position3Int, VoxelChangeData)> GetEnumerator()
        {
            foreach (KeyValuePair<Position3Int, VoxelChangeData> pair in m_ChangeMap)
                yield return (pair.Key, pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override string ToString() => $"Count: {m_ChangeMap.Count}";
    }

    public static class VoxelfieldComponents
    {
        public static readonly SessionElements SessionElements;

        static VoxelfieldComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.elements.AddRange(new[] {typeof(VoxelMapNameElement), typeof(ChangedVoxelsProperty)});
        }
    }
}