using System;
using System.IO;
using System.Reflection;
using System.Threading;

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
        private static readonly Mutex StreamMutex = new Mutex();

        static Serializer()
        {
            Stream.SetLength(Stream.Capacity);
        }

        private static void ReadIntoProperty(PropertyBase property)
        {
            property.Deserialize(Reader);
        }

        private static void WriteFromProperty(PropertyBase property)
        {
            property.Serialize(Writer);
        }

        public static void SerializeFrom(ComponentBase component, MemoryStream stream)
        {
            StreamMutex.WaitOne();
            try
            {
                Stream.Position = 0;
                Extensions.Navigate((field, property) =>
                {
                    if (!field.IsDefined(typeof(NoSerialization))) WriteFromProperty(property);
                }, component);
                var count = (int) Stream.Position;
                if (stream.Capacity < count)
                    stream.Capacity = count;
                Buffer.BlockCopy(Stream.GetBuffer(), 0, stream.GetBuffer(), (int) stream.Position, count);
                stream.Position = count;
            }
            finally
            {
                StreamMutex.ReleaseMutex();
            }
        }

        public static void DeserializeInto(ComponentBase component, MemoryStream stream)
        {
            StreamMutex.WaitOne();
            try
            {
                int count = stream.Capacity - (int) stream.Position;
                Buffer.BlockCopy(stream.GetBuffer(), (int) stream.Position, Stream.GetBuffer(), 0, count);
                Stream.Position = 0;
                Extensions.Navigate((field, property) =>
                {
                    if (!field.IsDefined(typeof(NoSerialization))) ReadIntoProperty(property);
                }, component);
            }
            finally
            {
                StreamMutex.ReleaseMutex();
            }
        }
    }
}