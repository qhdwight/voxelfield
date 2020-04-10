using System;
using System.Collections.Generic;
using System.Reflection;

namespace Components
{
    public abstract class ComponentBase
    {
        protected ComponentBase(Func<object> newInstance = null)
        {
            FieldInfo[] fieldInfos = Cache.GetFieldInfo(GetType());
            foreach (FieldInfo field in fieldInfos)
            {
                Type fieldType = field.FieldType;
                if (!fieldType.IsAbstract && field.GetValue(this) == null && (fieldType.IsProperty() || fieldType.IsComponent()))
                {
                    object instance = Activator.CreateInstance(fieldType);
                    field.SetValue(this, instance);
                    NewInstance(instance);
                }
            }
        }

        protected virtual void NewInstance(object instance)
        {
            
        }

        public virtual void InterpolateFrom(object c1, object c2, float interpolation)
        {
        }
    }
}