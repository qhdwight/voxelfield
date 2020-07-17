using System;
using System.Collections.Generic;
using LiteNetLib.Utils;

namespace Swihoni.Components
{
    [Serializable]
    public class DictProperty<TKey, TValue> : DictPropertyBase<TKey, TValue>
        where TKey : ElementBase where TValue : ElementBase
    {
        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_Map.Count);
            foreach (KeyValuePair<TKey, TValue> pair in m_Map)
            {
                pair.Key.Serialize(writer);
                pair.Value.Serialize(writer);
            }
        }

        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            for (var _ = 0; _ < count; _++)
            {
                var key = Activator.CreateInstance<TKey>();
                var value = Activator.CreateInstance<TValue>();
                key.Deserialize(reader);
                value.Deserialize(reader);
                m_Map.Add(key, value);
            }
            WithValue = true;
        }
    }

    [Serializable]
    public abstract class DictPropertyBase<TKey, TValue> : PropertyBase
    {
        protected Dictionary<TKey, TValue> m_Map = new Dictionary<TKey, TValue>();

        public Dictionary<TKey, TValue> Map => m_Map;

        public int Count => m_Map.Count;

        public override bool Equals(PropertyBase other) => throw new NotImplementedException();

        public override void Clear()
        {
            Zero();
            base.Clear();
        }

        public override void Zero() => m_Map.Clear();

        public override void SetFromIfWith(PropertyBase other)
        {
            if (other.WithValue) SetTo(other);
        }

        public override void InterpolateFromIfWith(PropertyBase p1, PropertyBase p2, float interpolation) => throw new Exception("Cannot interpolate dictionary");

        public override string ToString() => $"Count: {m_Map.Count}";

        public void AddAllFrom(DictPropertyBase<TKey, TValue> other)
        {
            foreach (KeyValuePair<TKey, TValue> pair in other.m_Map)
                Set(pair.Key, pair.Value);
        }

        public override void SetTo(PropertyBase other)
        {
            if (!(other is DictPropertyBase<TKey, TValue> otherMap)) throw new ArgumentException("Other was not same type map");
            Clear();
            AddAllFrom(otherMap);
            WithValue = true;
        }

        public TValue this[TKey key] => m_Map[key];

        public virtual void Set(in TKey key, in TValue value) => m_Map[key] = value;
    }
}