using System;
using System.Collections.Generic;
using System.Text;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Components
{
    [Serializable]
    public class ListProperty<T> : PropertyBase where T : ElementBase
    {
        private const string Separator = ";";

        [SerializeField] private List<T> m_List = new List<T>();
        [SerializeField] private int m_MaxSize;

        public IReadOnlyList<T> List => m_List;
        public int Count => m_List.Count;

        public ListProperty() { }

        public ListProperty(int maxSize) => m_MaxSize = maxSize;

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_List.Count);
            foreach (T element in m_List)
                element.Serialize(writer);
        }

        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            for (var _ = 0; _ < count; _++)
            {
                var element = Activator.CreateInstance<T>();
                element.Deserialize(reader);
                m_List.Add(element);
            }
            WithValue = true;
        }

        public override StringBuilder AppendValue(StringBuilder builder)
        {
            var afterFirst = false;
            foreach (T element in m_List)
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
                var element = Activator.CreateInstance<T>();
                element.Parse(elementString.Trim());
                Add(element);
            }
        }

        public override bool Equals(PropertyBase other) => throw new NotImplementedException();

        public override void Zero() => m_List.Clear();

        public override void SetTo(PropertyBase other)
        {
            if (!(other is ListProperty<T> otherList)) throw new ArgumentException("Other was not same type list");
            Clear();
            AddAllFrom(otherList);
        }

        public void AddAllFrom(ListProperty<T> other)
        {
            m_List.AddRange(other.m_List);
            WithValue = true;
        }

        public void Add(T element)
        {
            m_List.Add(element);
            WithValue = true;
            while (m_List.Count > m_MaxSize) m_List.RemoveAt(0);
        }

        public override void InterpolateFromIfWith(PropertyBase p1, PropertyBase p2, float interpolation) => throw new NotImplementedException();
    }
}