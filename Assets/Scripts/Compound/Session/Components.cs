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
    /* Session */

    [Serializable]
    public class VoxelMapNameProperty : StringProperty
    {
        public VoxelMapNameProperty() : base(16) { }
    }

    [Serializable, Additive]
    public class ChangedVoxelsProperty : PropertyBase, IVoxelChanges, IEnumerable<(Position3Int, VoxelChangeData)>
    {
        private Dictionary<Position3Int, VoxelChangeData> m_ChangeMap = new Dictionary<Position3Int, VoxelChangeData>();

        public Dictionary<Position3Int, VoxelChangeData> Unsafe => m_ChangeMap;

        public int Count => m_ChangeMap.Count;

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

        public override void Zero()
        {
            WithValue = false;
            m_ChangeMap.Clear();
        }

        public override void SetFromIfWith(PropertyBase other)
        {
            if (other.WithValue) SetTo(other);
        }

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

    [Serializable]
    public class ShowdownSessionComponent : ComponentBase
    {
        public ByteProperty number;
        public UIntProperty remainingUs;
    }

    /* Player */

    [Serializable]
    public class ShowdownPlayerComponent : ComponentBase
    {
        public ByteProperty cured;
    }

    [Serializable]
    public class Position3IntProperty : PropertyBase<Position3Int>
    {
        public override bool ValueEquals(PropertyBase<Position3Int> other) => other.Value == Value;
        public override void SerializeValue(NetDataWriter writer) => Position3Int.Serialize(Value, writer);
        public override void DeserializeValue(NetDataReader reader) => Position3Int.Deserialize(reader);
    }

    [Serializable]
    public class DesignerPlayerComponent : ComponentBase
    {
        public Position3IntProperty positionOne, positionTwo;
        public ByteProperty selectedBlockId;
    }

    [Serializable]
    public class MoneyProperty : UShortProperty
    {
    }

    public static class VoxelfieldComponents
    {
        public static readonly SessionElements SessionElements;

        static VoxelfieldComponents()
        {
            SessionElements = SessionElements.NewStandardSessionElements();
            SessionElements.playerElements.AppendAll(typeof(ShowdownPlayerComponent), typeof(DesignerPlayerComponent), typeof(MoneyProperty));
            // SessionElements.commandElements.AppendAll(typeof(TeamProperty));
            SessionElements.elements.AppendAll(typeof(VoxelMapNameProperty), typeof(ChangedVoxelsProperty), typeof(ShowdownSessionComponent));
        }
    }
}