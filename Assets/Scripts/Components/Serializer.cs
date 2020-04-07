using System;
using System.IO;
using System.Reflection;

namespace Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class NoSerialization : Attribute
    {
    }

    public static class Serializer
    {
        private static readonly MemoryStream Stream = new MemoryStream(1 << 16);
        private static readonly BinaryWriter Writer = new BinaryWriter(Stream);
        private static readonly BinaryReader Reader = new BinaryReader(Stream);

        private static void ReadIntoProperty(PropertyBase property)
        {
            property.Deserialize(Reader);
        }

        private static void WriteFromProperty(PropertyBase property)
        {
            property.Serialize(Writer);
        }

        public static void SerializeFrom(ComponentBase component, Stream stream)
        {
            Stream.Position = 0;
            Extensions.Navigate((field, property) =>
            {
                if (!field.IsDefined(typeof(NoSerialization))) WriteFromProperty(property);
            }, component);
            var length = (int) Stream.Position;
            Stream.Position = 0;
            Stream.CopyTo(stream, length);
        }

        public static void DeserializeInto(ComponentBase component, Stream stream)
        {
            Stream.Position = 0;
            stream.CopyTo(Stream);
            Stream.Position = 0;
            Extensions.Navigate((field, property) =>
            {
                if (!field.IsDefined(typeof(NoSerialization))) ReadIntoProperty(property);
            }, component);
        }
    }
}