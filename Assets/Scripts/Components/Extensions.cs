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

        public static bool IsContainable(this Type type)
        {
            return type.IsSubclassOf(typeof(ElementBase));
        }

        public static bool IsContainer(this Type type)
        {
            return typeof(Container).IsAssignableFrom(type);
        }

        /// <summary>
        /// Reset all properties to default values clear has value flags.
        /// </summary>
        public static void Reset(this ComponentBase o)
        {
            var @object = new TriArray<ElementBase> {[0] = o};
            Navigate((field, properties) => properties[0].Clear(), @object, 1);
        }

        /// <summary>
        /// Allocates a cloned instance. Do not use in loops.
        /// </summary>
        public static T Clone<T>(this T component) where T : ComponentBase
        {
            var clone = Activator.CreateInstance<T>();
            clone.MergeSet(component);
            return clone;
        }

        internal static void Navigate(Action<FieldInfo, PropertyBase> visitProperty, ElementBase e)
        {
            var zip = new TriArray<ElementBase> {[0] = e};
            Navigate((field, properties) => visitProperty(field, properties[0]), zip, 1);
        }

        internal static void NavigateZipped(Action<FieldInfo, PropertyBase, PropertyBase> visitProperty, ElementBase e1, ElementBase e2)
        {
            var zip = new TriArray<ElementBase> {[0] = e1, [1] = e2};
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1]), zip, 2);
        }

        internal static void NavigateZipped(Action<FieldInfo, PropertyBase, PropertyBase, PropertyBase> visitProperty, ElementBase e1, ElementBase e2, ElementBase e3)
        {
            var zip = new TriArray<ElementBase> {[0] = e1, [1] = e2, [2] = e3};
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1], properties[2]), zip, 3);
        }

        private static void Navigate(Action<FieldInfo, TriArray<PropertyBase>> visitProperty, in TriArray<ElementBase> zip, int size)
        {
            if (size <= 0) throw new ArgumentException("Size needs to be greater than zero");
            void NavigateRecursively(in TriArray<ElementBase> _zip, FieldInfo _field)
            {
                FieldInfo[] fields = null;
                Type type = null;
                for (var i = 0; i < size; i++)
                {
                    Type zipType = _zip[i].GetType();
                    FieldInfo[] zipFields = Cache.GetFieldInfo(zipType);
                    int count = zipFields.Length;
                    if (fields != null && count >= fields.Length) continue;
                    fields = zipFields;
                    type = zipType;
                }
                if (type.IsContainer())
                {
                    var zippedContainers = new TriArray<Container>();
                    for (var i = 0; i < size; i++)
                        zippedContainers[i] = (Container) _zip[i];
                    foreach (Type childType in zippedContainers[0].Children.Keys)
                    {
                        if (childType.IsProperty())
                        {
                            var zippedProperties = new TriArray<PropertyBase>();
                            for (var i = 0; i < size; i++)
                                zippedProperties[i] = (PropertyBase) zippedContainers[i].Children[childType];
                            visitProperty(null, zippedProperties);
                        }
                        else
                        {
                            var zippedChildren = new TriArray<ElementBase>();
                            for (var i = 0; i < size; i++)
                                zippedChildren[i] = zippedContainers[i].Children[childType];
                            NavigateRecursively(zippedChildren, null);
                        }
                    }
                }
                else if (type.IsArrayProperty())
                {
                    var zippedArrays = new TriArray<ArrayPropertyBase>();
                    for (var i = 0; i < size; i++)
                        zippedArrays[i] = (ArrayPropertyBase) _zip[i];
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
                            var zippedElements = new TriArray<ElementBase>();
                            for (var i = 0; i < size; i++)
                                zippedElements[i] = (ElementBase) zippedArrays[i].GetValue(j);
                            NavigateRecursively(zippedElements, _field);
                        }
                }
                else if (type.IsComponent())
                {
                    foreach (FieldInfo field in fields)
                    {
                        Type fieldType = field.FieldType;
                        if (fieldType.IsProperty())
                        {
                            var zippedProperties = new TriArray<PropertyBase>();
                            for (var i = 0; i < size; i++)
                                zippedProperties[i] = (PropertyBase) field.GetValue(_zip[i]);
                            visitProperty(field, zippedProperties);
                        }
                        else
                        {
                            var zippedChildren = new TriArray<ElementBase>();
                            for (var i = 0; i < size; i++)
                                zippedChildren[i] = (ElementBase) field.GetValue(_zip[i]);
                            NavigateRecursively(zippedChildren, field);
                        }
                    }
                }
                else
                {
                    throw new Exception("Expected component or array");
                }
            }
            NavigateRecursively(zip, null);
        }

        private struct TriArray<T>
        {
            private T e1, e2, e3;

            public T this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:  return e1;
                        case 1:  return e2;
                        case 2:  return e3;
                        default: throw new IndexOutOfRangeException();
                    }
                }
                set
                {
                    // @formatter:off
                    switch (index)
                    {
                        case 0: e1 = value; break;
                        case 1: e2 = value; break;
                        case 2: e3 = value; break;
                        default: throw new IndexOutOfRangeException();
                    }
                    // @formatter:on
                }
            }
        }
    }
}