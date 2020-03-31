using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Components
{
    public static class  Serializer
    {
        private static readonly MemoryStream Stream = new MemoryStream(1 << 16);
        private static readonly BinaryWriter Writer = new BinaryWriter(Stream);
        private static readonly BinaryReader Reader = new BinaryReader(Stream);

        private static readonly Dictionary<Type, Func<object>> Readers = new Dictionary<Type, Func<object>>
        {
            [typeof(int)] = () => Reader.ReadInt32(),
            [typeof(uint)] = () => Reader.ReadUInt32(),
            [typeof(float)] = () => Reader.ReadSingle(),
            [typeof(double)] = () => Reader.ReadDouble(),
            [typeof(Vector3)] = () => new Vector3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle())
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
                case int @int: Writer.Write(@int); break;
                case uint @uint: Writer.Write(@uint); break;
                case float @float: Writer.Write(@float); break;
                case double @double: Writer.Write(@double); break;
                case Vector3 vector: for (var i = 0; i < 3; i++) Writer.Write(vector[i]); break;
                default: throw new ArgumentException();
            }
            // @formatter:on
        }

        private static void Navigate(object @object, Action<PropertyBase> visitProperty)
        {
            void NavigateRecursively(object _object, Type _type)
            {
                if (_object == null)
                    throw new NullReferenceException("Null member");
                if (_type.IsComponent())
                {
                    foreach (FieldInfo field in Cache.GetFieldInfo(_type))
                    {
                        Type fieldType = field.FieldType;
                        if (fieldType.IsProperty())
                        {
                            var property = (PropertyBase) field.GetValue(_object);
                            visitProperty(property);
                        }
                        else
                        {
                            NavigateRecursively(field.GetValue(_object), fieldType);
                        }
                    }
                }
                else if (_type.IsArrayProperty())
                {
                    var array = (ArrayPropertyBase) _object;
                    Type elementType = array.GetElementType();
                    for (var i = 0; i < array.Length; i++)
                        if (elementType.IsProperty())
                        {
                            var property = (PropertyBase) array.GetValue(i);
                            visitProperty(property);
                        }
                        else
                        {
                            NavigateRecursively(array.GetValue(i), elementType);
                        }
                }
            }
            NavigateRecursively(@object, @object.GetType());
        }

        public static void SerializeFrom(object @object, Stream stream)
        {
            Stream.Position = 0;
            Navigate(@object, WriteFromProperty);
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
            Navigate(@object, ReadIntoProperty);
        }
    }
}