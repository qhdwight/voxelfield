using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Components
{
    public abstract class ArrayElementBase : ElementBase
    {
        public abstract int Length { get; }

        public abstract Type GetElementType { get; }

        public abstract ElementBase GetValue(int index);

        public ElementBase this[int index] => GetValue(index);
    }

    [Serializable]
    public class ArrayElement<TElement> : ArrayElementBase, IEnumerable<TElement> where TElement : ElementBase
    {
        [CopyField, SerializeField] public TElement[] m_Elements;

        public ArrayElement(params TElement[] values) => m_Elements = (TElement[]) values.Clone();

        public ArrayElement(int size)
        {
            m_Elements = new TElement[size];
            for (var i = 0; i < size; i++)
                m_Elements[i] = ComponentExtensions.NewElement<TElement>();
        }

        public void SetContainerTypes(params Type[] types) => SetContainerTypes((IEnumerable<Type>) types);

        public void SetContainerTypes(IEnumerable<Type> types)
        {
            ImmutableArray<Type> typesArray = types.ToImmutableArray();
            foreach (TElement element in m_Elements)
            {
                var container = (Container) (object) element;
                container.RegisterAppend(typesArray);
            }
        }

        public new TElement this[int index]
        {
            get => m_Elements[index];
            set => m_Elements[index] = value;
        }

        public override int Length => m_Elements.Length;

        public IEnumerator<TElement> GetEnumerator() => ((IEnumerable<TElement>) m_Elements).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => m_Elements.GetEnumerator();

        public override ElementBase GetValue(int index) => this[index];

        public override Type GetElementType => typeof(TElement);
    }

    [Serializable]
    public class CharProperty : PropertyBase<char>
    {
        public override bool ValueEquals(in char value) => value == Value;
        public override void SerializeValue(NetDataWriter writer) => writer.Put(Value);
        public override void DeserializeValue(NetDataReader reader) => Value = reader.GetChar();
    }
}