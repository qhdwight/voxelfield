using System;
using System.Reflection;

namespace Components
{
    public abstract class ComponentBase
    {
        protected ComponentBase()
        {
            FieldInfo[] fieldInfos = Cache.GetFieldInfo(GetType());
            foreach (FieldInfo fieldInfo in fieldInfos)
            {
                Type fieldType = fieldInfo.FieldType;
                if (fieldInfo.GetValue(this) == null && (fieldType.IsProperty() || fieldType.IsComponent()))
                    fieldInfo.SetValue(this, Activator.CreateInstance(fieldType));
            }
        }

        public virtual void InterpolateFrom(object c1, object c2, float interpolation)
        {
        }
    }
}