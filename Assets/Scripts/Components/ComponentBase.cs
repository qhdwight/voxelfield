using System;
using System.Reflection;
using System.Runtime.CompilerServices;

namespace Components
{
    public abstract class ElementBase
    {
        private bool Equals(ElementBase other)
        {
            return this.EqualTo(other);
        }

        public override bool Equals(object other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return other.GetType() == GetType() && Equals((ElementBase) other);
        }

        public override int GetHashCode()
        {
            //TODO: revisit
            return RuntimeHelpers.GetHashCode(this);
        }
    }

    public abstract class ComponentBase : ElementBase
    {
        protected ComponentBase()
        {
            FieldInfo[] fieldInfos = Cache.GetFieldInfo(GetType());
            foreach (FieldInfo field in fieldInfos)
            {
                Type fieldType = field.FieldType;
                if (!fieldType.IsAbstract && field.GetValue(this) == null && fieldType.IsElement())
                {
                    object instance = Activator.CreateInstance(fieldType);
                    field.SetValue(this, instance);
                }
            }
        }

        public virtual void InterpolateFrom(ComponentBase c1, ComponentBase c2, float interpolation)
        {
        }
    }
}