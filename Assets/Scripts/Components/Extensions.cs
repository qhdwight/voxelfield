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

        internal static void Navigate(Action<FieldInfo, PropertyBase> visitProperty, object o)
        {
            var @object = new ThreeReferences<object> {[0] = o};
            Navigate((field, properties) => visitProperty(field, properties[0]), @object, 1);
        }

        internal static void NavigateZipped(Action<FieldInfo, PropertyBase, PropertyBase> visitProperty, object o1, object o2)
        {
            var zipped = new ThreeReferences<object> {[0] = o1, [1] = o2};
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1]), zipped, 2);
        }

        internal static void NavigateZipped(Action<FieldInfo, PropertyBase, PropertyBase, PropertyBase> visitProperty, object o1, object o2, object o3)
        {
            var zipped = new ThreeReferences<object> {[0] = o1, [1] = o2, [2] = o3};
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1], properties[2]), zipped, 3);
        }

        private static void Navigate(Action<FieldInfo, ThreeReferences<PropertyBase>> visitProperty, in ThreeReferences<object> zipped, int size)
        {
            void NavigateRecursively(in ThreeReferences<object> _zipped, Type _type, FieldInfo _field)
            {
                ThreeReferences<object> zippedReferencesCopy = _zipped;
                for (var i = 0; i < size; i++)
                    if (_zipped[i] == null)
                        throw new NullReferenceException("Null member");
                    else
                        zippedReferencesCopy[i] = _zipped[i];
                if (_type.IsComponent())
                {
                    foreach (FieldInfo field in Cache.GetFieldInfo(_type))
                    {
                        Type fieldType = field.FieldType;
                        if (fieldType.IsProperty())
                        {
                            var zippedProperties = new ThreeReferences<PropertyBase>();
                            for (var i = 0; i < size; i++)
                                zippedProperties[i] = (PropertyBase) field.GetValue(_zipped[i]);
                            visitProperty(field, zippedProperties);
                        }
                        else
                        {
                            for (var i = 0; i < size; i++)
                                zippedReferencesCopy[i] = field.GetValue(_zipped[i]);
                            NavigateRecursively(zippedReferencesCopy, fieldType, field);
                        }
                    }
                }
                else if (_type.IsArrayProperty())
                {
                    var zippedArrays = new ThreeReferences<ArrayPropertyBase>();
                    for (var i = 0; i < size; i++)
                        zippedArrays[i] = (ArrayPropertyBase) _zipped[i];
                    Type elementType = zippedArrays[0].GetElementType();
                    bool isProperty = elementType.IsProperty();
                    for (var j = 0; j < zippedArrays[0].Length; j++)
                        if (isProperty)
                        {
                            var zippedProperties = new ThreeReferences<PropertyBase>();
                            for (var i = 0; i < size; i++)
                                zippedProperties[i] = (PropertyBase) zippedArrays[i].GetValue(j);
                            visitProperty(_field, zippedProperties);
                        }
                        else
                        {
                            for (var i = 0; i < size; i++)
                                zippedReferencesCopy[i] = zippedArrays[i].GetValue(j);
                            NavigateRecursively(zippedReferencesCopy, elementType, _field);
                        }
                }
                else
                {
                    throw new Exception("Expected component or array");
                }
            }
            Type type = zipped[0].GetType();
            NavigateRecursively(zipped, type, null);
        }

        private struct ThreeReferences<T>
        {
            private T o1, o2, o3;

            public T this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:  return o1;
                        case 1:  return o2;
                        case 2:  return o3;
                        default: throw new IndexOutOfRangeException();
                    }
                }
                set
                {
                    // @formatter:off
                    switch (index)
                    {
                        case 0: o1 = value; break;
                        case 1: o2 = value; break;
                        case 2: o3 = value; break;
                        default: throw new IndexOutOfRangeException();
                    }
                    // @formatter:on
                }
            }
        }
    }
}