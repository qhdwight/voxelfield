using System;
using System.Linq;
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

        public static void Navigate(Action<FieldInfo, PropertyBase[]> visitProperty, params object[] objects)
        {
            void NavigateRecursively(object[] _objects, Type _type, FieldInfo _field)
            {
                if (_objects.Any(_object => _object == null))
                    throw new NullReferenceException("Null member");
                if (_type.IsComponent())
                    foreach (FieldInfo field in Cache.GetFieldInfo(_type))
                    {
                        Type fieldType = field.FieldType;
                        if (fieldType.IsProperty())
                            visitProperty(field, _objects.Select(_object => (PropertyBase) field.GetValue(_object)).ToArray());
                        else
                            NavigateRecursively(_objects.Select(_object => field.GetValue(_object)).ToArray(), fieldType, field);
                    }
                else if (_type.IsArrayProperty())
                {
                    ArrayPropertyBase[] arrays = _objects.Cast<ArrayPropertyBase>().ToArray();
                    Type elementType = arrays.First().GetElementType();
                    bool isProperty = elementType.IsProperty();
                    for (var i = 0; i < arrays.First().Length; i++)
                    {
                        if (isProperty)
                        {
                            PropertyBase[] properties = arrays.Select(array => (PropertyBase) array.GetValue(i)).ToArray();
                            visitProperty(_field, properties);
                        }
                        else
                            NavigateRecursively(arrays.Select(array => array.GetValue(i)).ToArray(), elementType, _field);
                    }
                }
                else
                    throw new Exception("Expected component or array");
            }
            Type type = objects.First().GetType();
            NavigateRecursively(objects, type, null);
        }
    }
}