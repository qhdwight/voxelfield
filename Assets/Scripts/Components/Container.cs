using System;
using System.Collections.Generic;
using System.Linq;

namespace Components
{
    [Serializable]
    public class Container : ComponentBase
    {
        public IDictionary<Type, ElementBase> Children { get; } = new SortedDictionary<Type, ElementBase>();

        public Container(IEnumerable<Type> types) : this(types.ToArray())
        {
        }

        public Container(params Type[] types)
        {
            Add(types);
        }

        public void Add(params Type[] types)
        {
            foreach (Type type in types)
            {
                if (type.IsContainable())
                    Children[type] = (ElementBase) Activator.CreateInstance(type);
                else
                    throw new ArgumentException("Type must be containable (Property or Component)");
            }
        }

        public bool If<TElement>(out TElement component) where TElement : ElementBase
        {
            bool hasComponent = Children.TryGetValue(typeof(TElement), out ElementBase child);
            component = (TElement) child;
            return hasComponent;
        }

        public bool Has<TElement>()
        {
            return Children.ContainsKey(typeof(TElement));
        }

        public TElement Require<TElement>()
        {
            try
            {
                object child = Children[typeof(TElement)];
                return (TElement) child;
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Container does not have {typeof(TElement).Name}", exception);
            }
        }
    }
}