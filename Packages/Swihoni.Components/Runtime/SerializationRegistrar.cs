using System;
using System.Linq;
using Swihoni.Collections;

namespace Swihoni.Components
{
    public static class SerializationRegistrar
    {
        private static DualDictionary<Type, ushort> _typeToId;

        public static void RegisterAll(params Type[] types)
        {
            ushort counter = 0;
            _typeToId = new DualDictionary<Type, ushort>(types.ToDictionary(type => type, type => counter++));
        }

        public static ushort GetId<T>() => GetId(typeof(T));

        public static ushort GetId(Type type) => _typeToId.GetForward(type);

        public static Type GetType(ushort id) => _typeToId.GetReverse(id);
    }
}