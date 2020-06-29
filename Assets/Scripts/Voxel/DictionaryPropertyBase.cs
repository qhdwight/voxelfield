using System;
using System.Collections;
using System.Collections.Generic;
using Swihoni.Components;

namespace Voxel
{
    [Serializable]
    public abstract class DictionaryPropertyBase<TKey, TValue> : PropertyBase, IEnumerable<(TKey, TValue)>
    {
        protected Dictionary<TKey, TValue> m_Map = new Dictionary<TKey, TValue>();

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

        public IEnumerator<(TKey, TValue)> GetEnumerator()
        {
            foreach (KeyValuePair<TKey, TValue> pair in m_Map)
                yield return (pair.Key, pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public override void InterpolateFromIfWith(PropertyBase p1, PropertyBase p2, float interpolation) => throw new Exception("Cannot interpolate dictionary");

        public override string ToString() => $"Count: {m_Map.Count}";
    }
}