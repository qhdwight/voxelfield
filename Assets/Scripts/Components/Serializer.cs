using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Components
{
    public static class Serializer
    {
        private static readonly MemoryStream Stream = new MemoryStream(1 << 16);
        private static readonly BinaryWriter Writer = new BinaryWriter(Stream);
        private static readonly BinaryReader Reader = new BinaryReader(Stream);

        private static readonly Dictionary<Type, Func<object>> Readers = new Dictionary<Type, Func<object>>
        {
            // @formatter:off
            [typeof(    int)] = () => Reader.ReadInt32(),
            [typeof(   uint)] = () => Reader.ReadUInt32(),
            [typeof(  float)] = () => Reader.ReadSingle(),
            [typeof( double)] = () => Reader.ReadDouble(),
            [typeof(Vector3)] = () => new Vector3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle())
            // @formatter:on
        };

        private static void ReadIntoProperty(PropertyBase property)
        {
            property.SetValue(Readers[property.ValueType]());
        }

        private static void WriteFromProperty(PropertyBase property)
        {
            // @formatter:off
            switch (property.GetValue())
            {
                case    int     @int: Writer.Write(   @int); break;
                case    uint   @uint: Writer.Write(  @uint); break;
                case   float  @float: Writer.Write( @float); break;
                case  double @double: Writer.Write(@double); break;
                case Vector3  vector: for (var i = 0; i < 3; i++) Writer.Write(vector[i]); break;
                default: throw new ArgumentException();
            }
            // @formatter:on
        }

        public static void SerializeFrom(object @object, Stream stream)
        {
            Stream.Position = 0;
            Extensions.Navigate((_, property) => WriteFromProperty(property), @object);
            var length = (int) Stream.Position;
            Stream.Position = 0;
            stream.Position = 0;
            Stream.CopyTo(stream, length);
        }

        public static void DeserializeInto(object @object, Stream stream)
        {
            var length = (int) stream.Position;
            stream.Position = 0;
            Stream.Position = 0;
            stream.CopyTo(Stream, length);
            Stream.Position = 0;
            Extensions.Navigate((_, property) => ReadIntoProperty(property), @object);
        }
    }
}