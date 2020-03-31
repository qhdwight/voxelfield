using System;
using System.Reflection;

namespace Components
{
    public static class Extensions
    {
        public static bool IsComponent(this Type type)
        {
            return type.IsSubclassOf(typeof(ComponentBase));
        }

        public static bool IsProperty(this Type type)
        {
            return type.IsSubclassOf(typeof(PropertyBase));
        }

        public static bool IsArrayProperty(this Type type)
        {
            return type.IsSubclassOf(typeof(ArrayPropertyBase));
        }

        public static void Navigate(object o1, object o2, Action<PropertyBase, PropertyBase> visitProperty)
        {
            void NavigateRecursive(object _o1, object _o2, Type _type)
            {
                if (_o1 == null || _o2 == null)
                    throw new NullReferenceException("Null member");
                if (_type.IsComponent())
                    foreach (FieldInfo field in Cache.GetFieldInfo(_type))
                    {
                        Type fieldType = field.FieldType;
                        if (fieldType.IsProperty())
                        {
                            var sourceProperty = (PropertyBase) field.GetValue(_o1);
                            var mergedProperty = (PropertyBase) field.GetValue(_o2);
                            visitProperty(sourceProperty, mergedProperty);
                        }
                        else
                            NavigateRecursive(field.GetValue(_o1), field.GetValue(_o2), fieldType);
                    }
                else if (_type.IsArrayProperty())
                {
                    var sourceArray = (ArrayPropertyBase) _o1;
                    var mergedArray = (ArrayPropertyBase) _o2;
                    if (sourceArray.Length != mergedArray.Length)
                        throw new Exception("Unequal array lengths");
                    Type elementType = sourceArray.GetElementType();
                    bool isProperty = elementType.IsProperty();
                    for (var i = 0; i < sourceArray.Length; i++)
                    {
                        if (isProperty)
                        {
                            var sourceElement = (PropertyBase) sourceArray.GetValue(i);
                            var mergedElement = (PropertyBase) mergedArray.GetValue(i);
                            visitProperty(sourceElement, mergedElement);
                        }
                        else
                            NavigateRecursive(sourceArray.GetValue(i), mergedArray.GetValue(i), elementType);
                    }
                }
                else
                    throw new Exception("Expected component or array");
            }
            NavigateRecursive(o1, o2, o1.GetType());
        }
    }
}