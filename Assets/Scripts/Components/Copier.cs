using System;
using System.Reflection;

namespace Components
{
    [AttributeUsage(AttributeTargets.Field)]
    public class Copy : Attribute
    {
    }

    public static class Copier
    {
        public static void CopyTo(object source, object destination)
        {
            void RecurseCopy(object _source, object _destination, Type type)
            {
                if (_source == null || _destination == null)
                {
                    return;
                }
                foreach (FieldInfo fieldInfo in Cache.GetFieldInfo(type))
                {
                    Type fieldType = fieldInfo.FieldType;
                    if (fieldType.IsValueType)
                        fieldInfo.SetValue(_destination, fieldInfo.GetValue(_source));
                    else if (fieldType.IsArray)
                    {
                        var sourceArray = (Array) fieldInfo.GetValue(_source);
                        var destinationArray = (Array) fieldInfo.GetValue(_destination);
                        if (sourceArray.Length != destinationArray.Length) throw new Exception("Unequal array lengths");
                        Type elementType = sourceArray.GetType().GetElementType();
                        if (elementType.IsValueType)
                            Array.Copy(sourceArray, destinationArray, sourceArray.Length);
                        else
                            for (var i = 0; i < sourceArray.Length; i++)
                                CopyTo(sourceArray.GetValue(i), destinationArray.GetValue(i));
                    }
                    else
                        RecurseCopy(fieldInfo.GetValue(_source), fieldInfo.GetValue(_destination), fieldType);
                }
            }
            RecurseCopy(source, destination, source.GetType());
        }
    }
}