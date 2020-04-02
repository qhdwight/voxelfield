using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Components
{
    internal static class Cache
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();

        internal static FieldInfo[] GetFieldInfo(Type type)
        {
            if (!FieldCache.ContainsKey(type))
                FieldCache.Add(type,
                               type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                                   .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                               .Where(field => field.IsDefined(typeof(CopyField)))).ToArray());
            return FieldCache[type];
        }
    }
}