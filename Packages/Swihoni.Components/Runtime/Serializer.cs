using System;
using System.IO;
using System.Threading;

namespace Swihoni.Components
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

        static Serializer() { Stream.SetLength(Stream.Capacity); }

        public static void Serialize(this ElementBase component, MemoryStream stream)
        {
            StreamMutex.WaitOne();
            try
            {
                Stream.Position = 0;
                component.Navigate(element =>
                {
                    if (element is PropertyBase property)
                        property.Serialize(Writer);
                    return Navigation.Continue;
                });
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

        public static void Deserialize(this ElementBase component, MemoryStream stream, int? length = null)
        {
            StreamMutex.WaitOne();
            try
            {
                int count = stream.Capacity - (int) stream.Position;
                Buffer.BlockCopy(stream.GetBuffer(), (int) stream.Position, Stream.GetBuffer(), 0, count);
                Stream.Position = 0;
                component.Navigate(element =>
                {
                    if (element is PropertyBase property)
                    {
                        property.Clear();
                        property.Deserialize(Reader);
                    }
                    return Navigation.Continue;
                });
            }
            finally
            {
                StreamMutex.ReleaseMutex();
            }
        }
    }
}