using System;
using System.IO;
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

        public static void Zero(object o)
        {
            var @object = new TriArray<object> {[0] = o};
            Navigate((field, properties) => properties[0].Clear(), @object, 1);
        }

        internal static void Navigate(Action<FieldInfo, PropertyBase> visitProperty, object o)
        {
            var @object = new TriArray<object> {[0] = o};
            Navigate((field, properties) => visitProperty(field, properties[0]), @object, 1);
        }

        internal static void NavigateZipped(Action<FieldInfo, PropertyBase, PropertyBase> visitProperty, object o1, object o2)
        {
            var zipped = new TriArray<object> {[0] = o1, [1] = o2};
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1]), zipped, 2);
        }

        internal static void NavigateZipped(Action<FieldInfo, PropertyBase, PropertyBase, PropertyBase> visitProperty, object o1, object o2, object o3,
                                            Action<ComponentBase, ComponentBase, ComponentBase> visitComponent = null)
        {
            var zipped = new TriArray<object> {[0] = o1, [1] = o2, [2] = o3};
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1], properties[2]), zipped, 3,
                     visitComponent == null ? (Action<TriArray<ComponentBase>>) null : components => visitComponent(components[0], components[1], components[2]));
        }

        private static void Navigate(Action<FieldInfo, TriArray<PropertyBase>> visitProperty, in TriArray<object> zipped, int size,
                                     Action<TriArray<ComponentBase>> visitComponent = null)
        {
            if (size <= 0) throw new ArgumentException("Size needs to be greater than zero");
            void NavigateRecursively(in TriArray<object> _zipped, FieldInfo _field)
            {
                TriArray<object> zippedReferencesCopy = _zipped;
                for (var i = 0; i < size; i++)
                    if (_zipped[i] == null)
                        throw new NullReferenceException("Null member");
                    else
                        zippedReferencesCopy[i] = _zipped[i];
                FieldInfo[] _fields = null;
                Type _type = null;
                for (var i = 0; i < size; i++)
                {
                    Type type = _zipped[i].GetType();
                    FieldInfo[] fields = Cache.GetFieldInfo(type);
                    int count = fields.Length;
                    if (_fields != null && count >= _fields.Length) continue;
                    _fields = fields;
                    _type = type;
                }
                if (_type.IsComponent())
                {
                    if (visitComponent != null)
                    {
                        var zippedComponents = new TriArray<ComponentBase>();
                        for (var i = 0; i < size; i++)
                            zippedComponents[i] = (ComponentBase) _zipped[i];
                        visitComponent(zippedComponents);
                    }
                    foreach (FieldInfo field in _fields)
                    {
                        Type fieldType = field.FieldType;
                        if (fieldType.IsProperty())
                        {
                            var zippedProperties = new TriArray<PropertyBase>();
                            for (var i = 0; i < size; i++)
                                zippedProperties[i] = (PropertyBase) field.GetValue(_zipped[i]);
                            visitProperty(field, zippedProperties);
                        }
                        else
                        {
                            for (var i = 0; i < size; i++)
                                zippedReferencesCopy[i] = field.GetValue(_zipped[i]);
                            NavigateRecursively(zippedReferencesCopy, field);
                        }
                    }
                }
                else if (_type.IsArrayProperty())
                {
                    var zippedArrays = new TriArray<ArrayPropertyBase>();
                    for (var i = 0; i < size; i++)
                        zippedArrays[i] = (ArrayPropertyBase) _zipped[i];
                    Type elementType = zippedArrays[0].GetElementType();
                    bool isProperty = elementType.IsProperty();
                    for (var j = 0; j < zippedArrays[0].Length; j++)
                        if (isProperty)
                        {
                            var zippedProperties = new TriArray<PropertyBase>();
                            for (var i = 0; i < size; i++)
                                zippedProperties[i] = (PropertyBase) zippedArrays[i].GetValue(j);
                            visitProperty(_field, zippedProperties);
                        }
                        else
                        {
                            for (var i = 0; i < size; i++)
                                zippedReferencesCopy[i] = zippedArrays[i].GetValue(j);
                            NavigateRecursively(zippedReferencesCopy, _field);
                        }
                }
                else
                {
                    throw new Exception("Expected component or array");
                }
            }
            NavigateRecursively(zipped, null);
        }

        private struct TriArray<T>
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