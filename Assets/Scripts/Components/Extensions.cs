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
        public static void Reset(this ElementBase component)
        {
            var zip = new TriArray<ElementBase> {[0] = component};
            Navigate((field, properties) => (properties[0] as PropertyBase)?.Clear(), zip, 1);
        }

        /// <summary>
        /// Allocates a cloned instance. Do not use in loops.
        /// </summary>
        public static TElement Clone<TElement>(this TElement component) where TElement : ElementBase
        {
            var clone = (TElement) Activator.CreateInstance(component.GetType());
            if (clone is Container container)
                container.Add(((Container) (object) component).Children.Keys.ToArray());
            clone.MergeSet(component);
            return clone;
        }

        public static bool AreEquals<T>(this T component, T other) where T : ElementBase
        {
            var areEqual = true;
            NavigateZipped((field, e1, e2) =>
            {
                if (e1 is PropertyBase p1 && e2 is PropertyBase p2 && !p1.Equals(p2))
                    areEqual = false;
            }, component, other);
            return areEqual;
        }

        internal static void Navigate(Action<FieldInfo, ElementBase> visitProperty, ElementBase e)
        {
            var zip = new TriArray<ElementBase> {[0] = e};
            Navigate((field, properties) => visitProperty(field, properties[0]), zip, 1);
        }

        internal static void NavigateZipped(Action<FieldInfo, ElementBase, ElementBase> visitProperty, ElementBase e1, ElementBase e2)
        {
            var zip = new TriArray<ElementBase> {[0] = e1, [1] = e2};
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1]), zip, 2);
        }

        internal static void NavigateZipped(Action<FieldInfo, ElementBase, ElementBase, ElementBase> visitProperty, ElementBase e1, ElementBase e2, ElementBase e3)
        {
            var zip = new TriArray<ElementBase> {[0] = e1, [1] = e2, [2] = e3};
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1], properties[2]), zip, 3);
        }

        private static void Navigate(Action<FieldInfo, TriArray<ElementBase>> visit, in TriArray<ElementBase> zip, int size)
        {
            if (size <= 0) throw new ArgumentException("Size needs to be greater than zero");
            void NavigateRecursively(in TriArray<ElementBase> _zip, FieldInfo _field)
            {
                visit(_field, _zip);
                Type type = _zip[0].GetType();
                FieldInfo[] fields = Cache.GetFieldInfo(type);
                // Polymorphism get the top shared base by examining number of fields
                // for (var i = 0; i < size; i++)
                // {
                //     Type zipType = _zip[i].GetType();
                //     FieldInfo[] zipFields = Cache.GetFieldInfo(zipType);
                //     int count = zipFields.Length;
                //     if (fields != null && count >= fields.Length) continue;
                //     fields = zipFields;
                //     type = zipType;
                // }
                if (type.IsContainer())
                {
                    var zippedContainers = new TriArray<Container>();
                    for (var i = 0; i < size; i++)
                        zippedContainers[i] = (Container) _zip[i];
                    foreach (Type childType in zippedContainers[0].Children.Keys)
                    {
                        var zippedChildren = new TriArray<ElementBase>();
                        for (var i = 0; i < size; i++)
                        {
                            zippedContainers[i].Children.TryGetValue(childType, out ElementBase child);
                            zippedChildren[i] = child;
                        }
                        NavigateRecursively(zippedChildren, null);
                    }
                }
                else if (type.IsArrayProperty())
                {
                    var zippedArrays = new TriArray<ArrayPropertyBase>();
                    for (var i = 0; i < size; i++)
                        zippedArrays[i] = (ArrayPropertyBase) _zip[i];
                    for (var j = 0; j < zippedArrays[0].Length; j++)
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
                        var zippedChildren = new TriArray<ElementBase>();
                        for (var i = 0; i < size; i++)
                            zippedChildren[i] = _zip[i] == null ? null : (ElementBase) field.GetValue(_zip[i]);
                        NavigateRecursively(zippedChildren, field);
                    }
                }
                else if (!type.IsProperty())
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