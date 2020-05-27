using System;
using System.IO;
using System.Reflection;
using System.Threading;

namespace Swihoni.Components
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class NoSerialization : Attribute
    {
    }

    public static class Serializer
    {
        private static readonly MemoryStream Stream = new MemoryStream(1 << 16);
        private static readonly BinaryWriter Writer = new BinaryWriter(Stream);
        private static readonly BinaryReader Reader = new BinaryReader(Stream);
        private static readonly Mutex StreamMutex = new Mutex();

        static Serializer() => Stream.SetLength(Stream.Capacity);

        /// <summary>
        /// Serialize element into stream.
        /// </summary>
        public static void Serialize(this ElementBase element, MemoryStream stream)
        {
            StreamMutex.WaitOne();
            try
            {
                Stream.Position = 0;
                element.Navigate(_e =>
                {
                    switch (_e)
                    {
                        case ComponentBase _ when _e.GetType().IsDefined(typeof(NoSerialization)):
                            return Navigation.SkipDescendents;
                        case PropertyBase property:
                            property.Serialize(Writer);
                            break;
                    }
                    return Navigation.Continue;
                });
                var count = (int) Stream.Position;
                if (stream.Capacity < count) stream.Capacity = (int) (stream.Position + count);
                Buffer.BlockCopy(Stream.GetBuffer(), 0, stream.GetBuffer(), (int) stream.Position, count);
                stream.Position = count;
            }
            finally
            {
                StreamMutex.ReleaseMutex();
            }
        }

        /// <summary>
        /// Deserializes into element from stream.
        /// </summary>
        public static void Deserialize(this ElementBase element, MemoryStream stream, int? length = null)
        {
            StreamMutex.WaitOne();
            try
            {
                // TODO:performance use length parameter
                int count = stream.Capacity - (int) stream.Position;
                Buffer.BlockCopy(stream.GetBuffer(), (int) stream.Position, Stream.GetBuffer(), 0, count);
                Stream.Position = 0;
                element.Navigate(_e =>
                {
                    switch (_e)
                    {
                        case ComponentBase _ when _e.GetType().IsDefined(typeof(NoSerialization)):
                            return Navigation.SkipDescendents;
                        case PropertyBase property:
                            property.Clear();
                            property.Deserialize(Reader);
                            break;
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