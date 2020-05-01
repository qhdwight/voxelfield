using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Swihoni.Components
{
    public abstract class ElementBase
    {
        private bool Equals(ElementBase other) { return this.EqualTo(other); }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.GetType() == GetType() && Equals((ElementBase) other);
        }

        public override int GetHashCode() { return RuntimeHelpers.GetHashCode(this); }
    }

    public abstract class ComponentBase : ElementBase
    {
        private List<ElementBase> m_Elements;

        public IReadOnlyList<ElementBase> Elements
        {
            get
            {
                if (m_Elements == null)
                {
                    m_Elements = new List<ElementBase>();
                    FieldInfo[] fieldInfos = Cache.GetFieldInfo(GetType());
                    foreach (FieldInfo field in fieldInfos)
                    {
                        object fieldValue = field.GetValue(this);
                        if (fieldValue is ElementBase element) m_Elements.Add(element);
                    }
                }
                return m_Elements;
            }
        }

        protected ComponentBase()
        {
            foreach (FieldInfo field in Cache.GetFieldInfo(GetType()))
            {
                Type fieldType = field.FieldType;
                bool isElement = fieldType.IsElement();
                if (!isElement || fieldType.IsAbstract || field.GetValue(this) != null) continue;

                object instance = Activator.CreateInstance(fieldType);
                if (instance is PropertyBase propertyInstance) propertyInstance.Field = field;
                field.SetValue(this, instance);
            }
        }

        public virtual void InterpolateFrom(ComponentBase c1, ComponentBase c2, float interpolation) { }
    }
}