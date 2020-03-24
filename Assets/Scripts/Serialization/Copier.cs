using System;
using System.Collections.Generic;
using System.Reflection;

namespace Serialization
{
    public static class Copier
    {
        private static readonly Dictionary<Type, FieldInfo[]> Cache = new Dictionary<Type, FieldInfo[]>();
        
        public static void CopyTo<T>(T source, T destination) where T : class
        {
            FieldInfo[] GetFieldInfo(Type type)
            {
                if (!Cache.ContainsKey(type)) Cache.Add(type, type.GetFields());
                return Cache[type];
            }
            void RecurseCopy(object src, object dest, Type type)
            {
                foreach (FieldInfo fieldInfo in GetFieldInfo(type))
                {
                    Type fieldType = fieldInfo.FieldType;
                    if (fieldType.IsValueType)
                    {
                        fieldInfo.SetValue(dest, fieldInfo.GetValue(src));
                    }
                    else
                    {
                        RecurseCopy(fieldInfo.GetValue(src), fieldInfo.GetValue(dest), fieldType);
                    }
                }
            }
            RecurseCopy(source, destination, typeof(T));
        }
    }
}