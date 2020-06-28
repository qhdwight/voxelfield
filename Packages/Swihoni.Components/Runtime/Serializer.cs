using System;
using System.Reflection;
using LiteNetLib.Utils;

namespace Swihoni.Components
{
    [AttributeUsage(AttributeTargets.Field | AttributeTargets.Class)]
    public class NoSerialization : Attribute
    {
    }

    public static class Serializer
    {
        /// <summary>
        /// Serialize element into stream.
        /// </summary>
        public static void Serialize(this ElementBase element, NetDataWriter writer)
        {
            element.Navigate(_element =>
            {
                if (_element.WithAttribute<NoSerialization>()) return Navigation.SkipDescendents;
                if (_element is PropertyBase property) property.Serialize(writer);
                return Navigation.Continue;
            });
        }

        /// <summary>
        /// Deserializes into element from stream.
        /// </summary>
        public static void Deserialize(this ElementBase element, NetDataReader reader)
        {
            element.Navigate(_element =>
            {
                if (_element.WithAttribute<NoSerialization>()) return Navigation.SkipDescendents;
                if (_element is PropertyBase property)
                {
                    property.Clear();
                    property.Deserialize(reader);
                }
                return Navigation.Continue;
            });
        }
    }
}