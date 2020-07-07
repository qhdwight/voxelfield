using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util.Math;

namespace Voxel.Map
{
    [Serializable]
    public class ModelIdProperty : UShortProperty
    {
        public ModelIdProperty(ushort value) : base(value) { }
        public ModelIdProperty() { }
    }

    [Serializable]
    public class ModelsProperty : DictionaryPropertyBase<Position3Int, Container>
    {
        public const ushort Spawn = 0, Tree = 1, Cure = 2, Flag = 3, Last = 3;

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_Map.Count);
            foreach ((Position3Int position, Container container) in this)
            {
                Position3Int.Serialize(position, writer);
                var typeCount = (byte) container.Elements.Count;
                writer.Put(typeCount);
                SerializeContainer(container, writer);
            }
        }

        private static void SerializeContainer(Container container, NetDataWriter writer)
        {
            foreach ((Type type, ElementBase element) in container)
            {
                writer.Put(SerializationRegistrar.GetId(type));
                element.Serialize(writer);
            }
        }

        public override void Deserialize(NetDataReader reader)
        {
            Clear();
            int count = reader.GetInt();
            for (var _ = 0; _ < count; _++)
                m_Map.Add(Position3Int.Deserialize(reader), DeserializeContainer(reader));
            WithValue = true;
        }

        private static Container DeserializeContainer(NetDataReader reader)
        {
            byte typeCount = reader.GetByte();
            var container = new Container();
            for (var _ = 0; _ < typeCount; _++)
            {
                Type type = SerializationRegistrar.GetType(reader.GetUShort());
                var element = (ElementBase) Activator.CreateInstance(type);
                element.Deserialize(reader);
                container.Append(element);
            }
            return container;
        }

        public override void SetTo(PropertyBase other)
        {
            if (!(other is ModelsProperty otherModels)) throw new ArgumentException("Other was not models");
            Clear();
            foreach ((Position3Int position, Container container) in otherModels)
                Add(position, container);
        }

        public void Add(in Position3Int position, Container container) => m_Map.Add(position, container);

        public void Remove(in Position3Int position) => m_Map.Remove(position);
    }
}