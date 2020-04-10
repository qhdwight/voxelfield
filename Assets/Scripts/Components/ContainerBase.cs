using System;
using System.Collections.Generic;

namespace Components
{
    public abstract class ContainerBase : ComponentBase
    {
        private readonly Dictionary<Type, ComponentBase> m_Components = new Dictionary<Type, ComponentBase>();
        private readonly Dictionary<Type, PropertyBase> m_Properties = new Dictionary<Type, PropertyBase>();

        protected override void NewInstance(object instance)
        {
            switch (instance)
            {
                case ComponentBase component:
                    m_Components[instance.GetType()] = component;
                    break;
                case PropertyBase property:
                    m_Properties[property.GetType()] = property;
                    break;
            }
        }

        public bool WithComponent<TComponent>(out TComponent component) where TComponent : ComponentBase
        {
            bool hasComponent = m_Components.TryGetValue(typeof(TComponent), out ComponentBase childComponent);
            component = (TComponent) childComponent;
            return hasComponent;
        }

        public bool WithProperty<TProperty>(out TProperty component) where TProperty : PropertyBase
        {
            bool hasProperty = m_Properties.TryGetValue(typeof(TProperty), out PropertyBase childProperty);
            component = (TProperty) childProperty;
            return hasProperty;
        }
    }
}