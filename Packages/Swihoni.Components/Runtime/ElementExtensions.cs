using System;

namespace Swihoni.Components
{
    public enum Navigation
    {
        Continue,
        SkipDescendends, // Skip all children of current node
        Exit             // Exit entire tree immediately
    }

    public static class ElementExtensions
    {
        public static bool IsElement(this Type type) { return type.IsSubclassOf(typeof(ElementBase)); }

        /// <summary>
        /// Reset all properties to default values clear has value flags.
        /// </summary>
        public static void Reset(this ElementBase component)
        {
            component.Navigate(property =>
            {
                (property as PropertyBase)?.Clear();
                return Navigation.Continue;
            });
        }

        /// <summary>
        /// Reset all properties to default values with has value flag set.
        /// </summary>
        public static T Zero<T>(this T component) where T : ElementBase
        {
            component.Navigate(property =>
            {
                (property as PropertyBase)?.Zero();
                return Navigation.Continue;
            });
            return component;
        }

        /// <summary>
        /// Allocates a cloned instance. Do not use in loops.
        /// </summary>
        public static TElement Clone<TElement>(this TElement component) where TElement : ElementBase
        {
            var clone = (TElement) Activator.CreateInstance(component.GetType());
            NavigateZipped((e1, e2) =>
            {
                if (e1 is Container p1 && e2 is Container p2)
                    p2.TakeElementTypes(p1);
                return Navigation.Continue;
            }, component, clone);
            clone.FastMergeSet(component);
            return clone;
        }

        public static bool EqualTo<T>(this T component, T other) where T : ElementBase
        {
            var areEqual = true;
            NavigateZipped((e1, e2) =>
            {
                if (e1 is PropertyBase p1 && e2 is PropertyBase p2)
                {
                    if (p1.Equals(p2))
                        return Navigation.Continue;
                    areEqual = false;
                    return Navigation.Exit;
                }
                return Navigation.Continue;
            }, component, other);
            return areEqual;
        }

        public static void Navigate(this ElementBase e, Func<ElementBase, Navigation> visitProperty)
        {
            var zip = new TriArray<ElementBase> {[0] = e};
            Navigate(properties => visitProperty(properties[0]), zip, 1);
        }

        public static void NavigateZipped(Func<ElementBase, ElementBase, Navigation> visitProperty, ElementBase e1, ElementBase e2)
        {
            var zip = new TriArray<ElementBase> {[0] = e1, [1] = e2};
            Navigate(properties => visitProperty(properties[0], properties[1]), zip, 2);
        }

        public static void NavigateZipped(Func<ElementBase, ElementBase, ElementBase, Navigation> visitProperty, ElementBase e1, ElementBase e2, ElementBase e3)
        {
            var zip = new TriArray<ElementBase> {[0] = e1, [1] = e2, [2] = e3};
            Navigate(properties => visitProperty(properties[0], properties[1], properties[2]), zip, 3);
        }

        private static void Navigate(Func<TriArray<ElementBase>, Navigation> visit, in TriArray<ElementBase> zip, int size)
        {
            if (size <= 0) throw new ArgumentException("Size needs to be greater than zero");
            var exitAll = false;
            void NavigateRecursively(in TriArray<ElementBase> _zip)
            {
                Navigation navigation = visit(_zip);
                if (navigation == Navigation.Exit)
                    exitAll = true;
                if (exitAll || navigation == Navigation.SkipDescendends)
                    return;
                switch (_zip[0])
                {
                    case Container _:
                    {
                        var zippedContainers = new TriArray<Container>();
                        // Truncate iteration to smallest
                        var elementSize = int.MaxValue;
                        for (var i = 0; i < size; i++)
                        {
                            zippedContainers[i] = (Container) _zip[i];
                            int count = zippedContainers[i].Elements.Count;
                            if (count < elementSize) elementSize = count;
                        }
                        // Truncate iteration if elements become different types
                        var foundDifferentType = false;
                        for (var j = 0; j < elementSize && !foundDifferentType; j++)
                        {
                            Type firstType = zippedContainers[0].Elements[j].GetType();
                            for (var i = 1; i < size && !foundDifferentType; i++)
                            {
                                if (zippedContainers[i].Elements[j].GetType() == firstType) continue;
                                elementSize = j;
                                foundDifferentType = true;
                            }
                        }

                        for (var j = 0; j < elementSize; j++)
                        {
                            var zippedChildren = new TriArray<ElementBase>();
                            for (var i = 0; i < size; i++) zippedChildren[i] = zippedContainers[i].Elements[j];
                            NavigateRecursively(zippedChildren);
                        }
                        break;
                    }
                    case ArrayPropertyBase a1:
                    {
                        for (var j = 0; j < a1.Length; j++)
                        {
                            var zippedElements = new TriArray<ElementBase>();
                            for (var i = 0; i < size; i++) zippedElements[i] = (ElementBase) ((ArrayPropertyBase) _zip[i]).GetValue(j);
                            NavigateRecursively(zippedElements);
                        }
                        break;
                    }
                    case ComponentBase c1:
                    {
                        for (var j = 0; j < c1.Elements.Count; j++)
                        {
                            var zippedChildren = new TriArray<ElementBase>();
                            for (var i = 0; i < size; i++) zippedChildren[i] = ((ComponentBase) _zip[i]).Elements[j];
                            NavigateRecursively(zippedChildren);
                        }
                        break;
                    }
                    default:
                    {
                        if (!(_zip[0] is PropertyBase)) throw new Exception("Expected component or array");
                        break;
                    }
                }
            }
            NavigateRecursively(zip);
        }

        private struct TriArray<T>
        {
            private T m_E1, m_E2, m_E3;

            public T this[int index]
            {
                get
                {
                    switch (index)
                    {
                        case 0:  return m_E1;
                        case 1:  return m_E2;
                        case 2:  return m_E3;
                        default: throw new IndexOutOfRangeException();
                    }
                }
                set
                {
                    // @formatter:off
                    switch (index)
                    {
                        case 0:  m_E1 = value; break;
                        case 1:  m_E2 = value; break;
                        case 2:  m_E3 = value; break;
                        default: throw new IndexOutOfRangeException();
                    }
                    // @formatter:on
                }
            }
        }
    }
}