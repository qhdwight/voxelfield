using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Swihoni.Components
{
    public static class ReflectionCache
    {
        private static readonly Dictionary<Type, FieldInfo[]> FieldCache = new Dictionary<Type, FieldInfo[]>();
        private static readonly Dictionary<MemberInfo, Dictionary<Type, Attribute>> AttributeCache = new Dictionary<MemberInfo, Dictionary<Type, Attribute>>();
        private static readonly Dictionary<MemberInfo, HashSet<Type>> AttributeTypeCache = new Dictionary<MemberInfo, HashSet<Type>>();

        /// <returns>Ordered list by declaring type of fields. This places fields belonging to base classes first.</returns>
        public static IReadOnlyList<FieldInfo> GetFieldInfo(Type type)
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

        public static bool TryAttribute<T>(MemberInfo member, out T attribute) where T : Attribute
        {
            Type attributeType = typeof(T);
            if (!AttributeTypeCache.TryGetValue(member, out HashSet<Type> types))
            {
                // Occurs only with first test
                var set = new Dictionary<Type, Attribute>();
                AttributeCache[member] = set;
                AttributeTypeCache[member] = types = new HashSet<Type>();
                attribute = null;
                foreach (Attribute memberAttributes in member.GetCustomAttributes())
                {
                    Type memberAttributeType = memberAttributes.GetType();
                    if (memberAttributeType == attributeType)
                        attribute = (T) memberAttributes;
                    set.Add(memberAttributeType, memberAttributes);
                    types.Add(memberAttributeType);
                }
                return attribute != null;
            }
            if (types.Contains(attributeType))
            {
                attribute = (T) AttributeCache[member][attributeType];
                return true;
            }
            attribute = default;
            return false;
        }
    }
}