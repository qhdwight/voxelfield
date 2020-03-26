using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Serialization
{
    public static class Copier
    {
        public static void CopyTo(object source, object destination)
        {
            void RecurseCopy(object src, object dest, Type type)
            {
                foreach (FieldInfo fieldInfo in Cache.GetFieldInfo(type))
                {
                    Type fieldType = fieldInfo.FieldType;
                    if (fieldType.IsValueType)
                        fieldInfo.SetValue(dest, fieldInfo.GetValue(src));
                    else if (fieldType.IsGenericType && fieldType.GetGenericTypeDefinition() == typeof(List<>))
                    {
                        var sourceList = (IList) fieldInfo.GetValue(src);
                        var destinationList = (IList) fieldInfo.GetValue(dest);
                        Type elementType = fieldType.GetGenericArguments().First();
                        for (var i = 0; i < sourceList.Count; i++)
                        {
                            if (elementType.IsValueType)
                                destinationList[i] = sourceList[i];
                            else if (elementType == typeof(string))
                                destinationList[i] = string.Copy((string) sourceList[i]);
                            else
                                CopyTo(sourceList[i], destinationList[i]);
                        }
                    }
                    else if (fieldType == typeof(string))
                        fieldInfo.SetValue(dest, string.Copy((string) fieldInfo.GetValue(src)));
                    else
                        RecurseCopy(fieldInfo.GetValue(src), fieldInfo.GetValue(dest), fieldType);
                }
            }
            RecurseCopy(source, destination, source.GetType());
        }
    }
}