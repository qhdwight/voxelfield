using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util.Math;

namespace Voxels.Map
{
    [Serializable]
    public class ModelIdProperty : UShortProperty
    {
        public ModelIdProperty(ushort value) : base(value) { }
        public ModelIdProperty() { }
    }

    [Serializable]
    public class ModelsProperty : DictPropertyBase<Position3Int, Container>
    {
        public const ushort Spawn = 0, Tree = 1, Cure = 2, Flag = 3, Barrel = 4, Crate = 5, Site = 6, Health = 7, Ammo = 8;

        public override void Serialize(NetDataWriter writer)
        {
            writer.Put(m_Map.Count);
            foreach (KeyValuePair<Position3Int, Container> pair in m_Map)
            {
                Position3Int.Serialize(pair.Key, writer);
                var typeCount = (byte) pair.Value.Elements.Count;
                writer.Put(typeCount);
                SerializeContainer(pair.Value, writer);
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

        public void Add(in Position3Int position, Container container) => m_Map.Add(position, container);

        public void Remove(in Position3Int position) => m_Map.Remove(position);
    }
}