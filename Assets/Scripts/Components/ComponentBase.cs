using System;
using System.Reflection;

namespace Components
{
    public abstract class ContainableBase
    {
    }

    public abstract class ComponentBase : ContainableBase
    {
        protected ComponentBase()
        {
            FieldInfo[] fieldInfos = Cache.GetFieldInfo(GetType());
            foreach (FieldInfo field in fieldInfos)
            {
                Type fieldType = field.FieldType;
                if (!fieldType.IsAbstract && field.GetValue(this) == null && fieldType.IsContainable())
                {
                    object instance = Activator.CreateInstance(fieldType);
                    field.SetValue(this, instance);
                }
            }
        }

        public virtual void InterpolateFrom(object c1, object c2, float interpolation)
        {
        }
    }
}