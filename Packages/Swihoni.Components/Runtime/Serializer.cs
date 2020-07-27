using System;
using System.Text;
using LiteNetLib.Utils;

namespace Swihoni.Components
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class NoSerializationAttribute : Attribute
    {
        public bool ExceptRead { get; }
        public bool ExceptWrite { get; }

        public NoSerializationAttribute(bool exceptRead = false, bool exceptWrite = false)
        {
            if (exceptRead && exceptWrite) throw new ArgumentException("Remove attribute if reading and writing are enabled");
            ExceptRead = exceptRead;
            ExceptWrite = exceptWrite;
        }
    }

    public static class Serializer
    {
        private static NetDataWriter _writer;
        private static NetDataReader _reader;
        private static StringBuilder _stringBuilder;

        /// <summary>
        /// Serialize element into stream.
        /// </summary>
        public static void Serialize(this ElementBase element, NetDataWriter writer)
        {
            _writer = writer; // Prevents heap allocation in closure
            element.Navigate(_element =>
            {
                if (_element.TryAttribute(out NoSerializationAttribute attribute) && !attribute.ExceptWrite) return Navigation.SkipDescendents;
                if (_element is PropertyBase property) property.Serialize(_writer);
                return Navigation.Continue;
            });
        }

        /// <summary>
        /// Deserializes into element from stream.
        /// </summary>
        public static ElementBase Deserialize(this ElementBase element, NetDataReader reader)
        {
            _reader = reader;
            element.Navigate(_element =>
            {
                if (_element.TryAttribute(out NoSerializationAttribute attribute) && !attribute.ExceptRead) return Navigation.SkipDescendents;
                if (_element is PropertyBase property)
                {
                    property.Clear();
                    property.Deserialize(_reader);
                }
                return Navigation.Continue;
            });
            return element;
        }
    }
}