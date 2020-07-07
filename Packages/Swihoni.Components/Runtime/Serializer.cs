using System;
using LiteNetLib.Utils;

namespace Swihoni.Components
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class NoSerialization : Attribute
    {
    }

    public static class Serializer
    {
        private static NetDataWriter _writer;
        private static NetDataReader _reader;

        /// <summary>
        /// Serialize element into stream.
        /// </summary>
        public static void Serialize(this ElementBase element, NetDataWriter writer)
        {
            _writer = writer; // Prevents heap allocation in closure
            element.Navigate(_element =>
            {
                if (_element.WithAttribute<NoSerialization>()) return Navigation.SkipDescendents;
                if (_element is PropertyBase property) property.Serialize(_writer);
                return Navigation.Continue;
            });
        }

        /// <summary>
        /// Deserializes into element from stream.
        /// </summary>
        public static void Deserialize(this ElementBase element, NetDataReader reader)
        {
            _reader = reader;
            element.Navigate(_element =>
            {
                if (_element.WithAttribute<NoSerialization>()) return Navigation.SkipDescendents;
                if (_element is PropertyBase property)
                {
                    property.Clear();
                    property.Deserialize(_reader);
                }
                return Navigation.Continue;
            });
        }
    }
}