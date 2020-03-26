using System;
using System.Collections.Generic;
using System.Reflection;

namespace Serialization
{
    internal static class Cache
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();

        internal static FieldInfo[] GetFieldInfo(Type type)
        {
            if (!FieldCache.ContainsKey(type)) FieldCache.Add(type, type.GetFields());
            return FieldCache[type];
        }
    }
}