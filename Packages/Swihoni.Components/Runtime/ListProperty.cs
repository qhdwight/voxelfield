using System;
using System.Collections.Generic;
using System.Text;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Components
{
    [Serializable]
    public abstract class ListPropertyBase<TElement> : PropertyBase
    {
        [SerializeField] protected List<TElement> m_List = new();
        [SerializeField] private int m_MaxSize;

        public IReadOnlyList<TElement> List => m_List;
        public int Count => m_List.Count;

        public ListPropertyBase() { }
        public ListPropertyBase(int maxSize) => m_MaxSize = maxSize;

        public override void Clear()
        {
            Zero();
            base.Clear();
        }

        public override void Zero() => m_List.Clear();

        public override void SetTo(PropertyBase other)
        {
            if (!(other is ListPropertyBase<TElement> otherList)) throw new ArgumentException("Other was not same type list");
            Clear();
            AddAllFrom(otherList);
        }

        public void AddAllFrom(ListPropertyBase<TElement> other)
        {
            m_List.AddRange(other.m_List);
            WithValue = true;
        }

        public void Append(TElement element)
        {
            m_List.Add(element);
            WithValue = true;
            while (m_List.Count > m_MaxSize)
            {
                m_List.RemoveAt(0);
#if UNITY_EDITOR
                Debug.LogWarning($"Had to remove since over max size: {m_MaxSize}");
#endif
            }
        }

        public void RemoveEnd() => m_List.RemoveAt(m_List.Count - 1);

        public bool TryRemoveEnd(out TElement element)
        {
            int index = m_List.Count - 1;
            if (index < 0)
            {
                element = default;
                return false;
            }
            element = m_List[index];
            m_List.RemoveAt(index);
            return true;
        }
    }

    [Serializable]
    public class ListProperty<TElement> : ListPropertyBase<TElement> where TElement : ElementBase
    {
        private const string Separator = ";";

        public ListProperty() { }
        public ListProperty(int maxSize) : base(maxSize) { }

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_List.Count);
            foreach (TElement element in m_List)
                element.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            for (var _ = 0; _ < count; _++)
            {
                var element = ComponentExtensions.NewElement<TElement>();
                element.Deserialize(reader);
                m_List.Add(element);
            }
            WithValue = true;
        }

        public override bool Equals(PropertyBase other)
        {
            if (!(other is ListProperty<TElement> otherList)) throw new ArgumentException("Other was not same type list");
            if (otherList.Count != Count) return false;
            for (var i = 0; i < Count; i++)
                if (!Equals(m_List[i], otherList.m_List[i]))
                    return false;
            return true;
        }

        public override StringBuilder AppendValue(StringBuilder builder)
        {
            var afterFirst = false;
            foreach (TElement element in m_List)
            {
                if (afterFirst) builder.Append(Separator).Append(" ");
                builder.Stringify(element);
                afterFirst = true;
            }
            return builder;
        }

        public override void ParseValue(string stringValue)
        {
            Zero();
            string[] elementStrings = stringValue.Split(new[] {Separator}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string elementString in elementStrings)
            {
                var element = ComponentExtensions.NewElement<TElement>();
                element.Parse(elementString.Trim());
                Append(element);
            }
        }
    }
}