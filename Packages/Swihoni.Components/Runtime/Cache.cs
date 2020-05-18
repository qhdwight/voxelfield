using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Swihoni.Components
{
    internal static class Cache
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();

        /// <returns>Ordered list by declaring type of fields. This places fields belonging to base classes first.</returns>
        internal static IReadOnlyList<FieldInfo> GetFieldInfo(Type type)
        {
            if (!FieldCache.ContainsKey(type))
                FieldCache.Add(type,
                               type.GetFields(BindingFlags.Instance | BindingFlags.Public)
                                   .Concat(type.GetFields(BindingFlags.Instance | BindingFlags.NonPublic)
                                               .Where(field => field.IsDefined(typeof(CopyField))))
                                   .GroupBy(field => field.DeclaringType)
                                   .Reverse()
                                   .SelectMany(grouping => grouping)
                                   .ToArray());
            return FieldCache[type];
        }
    }
}