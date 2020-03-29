using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UnityEngine;

namespace Serialization
{
    public static class Serializer
    {
        private static readonly MemoryStream Stream = new MemoryStream(1 << 16);
        private static readonly BinaryWriter Writer = new BinaryWriter(Stream);
        private static readonly BinaryReader Reader = new BinaryReader(Stream);

        private static void WriteValue(object primitive)
        {
            // @formatter:off
            switch (primitive)
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

        private static readonly Dictionary<Type, Func<object>> Readers = new Dictionary<Type, Func<object>>
        {
            [typeof(int)] = () => Reader.ReadInt32(),
            [typeof(uint)] = () => Reader.ReadUInt32(),
            [typeof(float)] = () => Reader.ReadSingle(),
            [typeof(double)] = () => Reader.ReadDouble(),
            [typeof(Vector3)] = () => new Vector3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle()),
        };

        private static object ReadValue(Type type)
        {
            return Readers[type]();
        }

        public static void Serialize(object @object, Stream stream)
        {
            Stream.Seek(0, SeekOrigin.Begin);
            void RecurseSerialize(object _object, Type _type)
            {
                foreach (FieldInfo fieldInfo in Cache.GetFieldInfo(_type))
                {
                    Type fieldType = fieldInfo.FieldType;
                    if (fieldType.IsValueType)
                        WriteValue(fieldInfo.GetValue(_object));
                    else if (fieldType.IsArray)
                    {
                        var array = (Array) fieldInfo.GetValue(_object);
                        Type elementType = array.GetType().GetElementType();
                        Writer.Write(array.Length);
                        for (var i = 0; i < array.Length; i++)
                            if (elementType.IsValueType)
                                WriteValue(array.GetValue(i));
                            else
                                Serialize(array.GetValue(i), stream);
                    }
                    else
                        RecurseSerialize(fieldInfo.GetValue(_object), fieldType);
                }
            }
            RecurseSerialize(@object, @object.GetType());
            Stream.CopyTo(stream);
        }

        public static void DeserializeInto(object @object, Stream stream)
        {
            stream.CopyTo(Stream);
            Stream.Seek(0, SeekOrigin.Begin);
            void RecurseDeserialize(object _object)
            {
                foreach (FieldInfo fieldInfo in Cache.GetFieldInfo(_object.GetType()))
                {
                    Type fieldType = fieldInfo.FieldType;
                    if (fieldType.IsValueType)
                        fieldInfo.SetValue(_object, ReadValue(fieldType));
                    else if (fieldType.IsArray)
                    {
                        var array = (Array) fieldInfo.GetValue(_object);
                        Type elementType = array.GetType().GetElementType();
                        int length = Reader.ReadInt32();
                        for (var i = 0; i < length; i++)
                            if (elementType.IsValueType)
                                array.SetValue(ReadValue(elementType), i);
                            else
                                Serialize(array.GetValue(i), stream);
                    }
                    else
                        RecurseDeserialize(fieldInfo.GetValue(_object));
                }
            }
            RecurseDeserialize(@object);
        }
    }
}