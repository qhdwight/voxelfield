using System;
using System.Collections.Generic;
using System.Linq;

namespace Components
{
    public class Container : ComponentBase
    {
        private readonly IDictionary<Type, ContainableBase> m_Children = new SortedDictionary<Type, ContainableBase>();

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
                    m_Children[type] = (ContainableBase) Activator.CreateInstance(type);
                else
                    throw new ArgumentException("Type must be containable (Property or Component)");
            }
        }

        public bool With<TComponent>(out TComponent component) where TComponent : ContainableBase
        {
            bool hasComponent = m_Children.TryGetValue(typeof(TComponent), out ContainableBase childComponent);
            component = (TComponent) childComponent;
            return hasComponent;
        }
    }
}