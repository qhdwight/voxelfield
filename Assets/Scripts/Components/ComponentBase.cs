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
                bool isProperty = fieldType.IsSubclassOf(typeof(PropertyBase)),
                     isComponent = fieldType.IsSubclassOf(typeof(ComponentBase));
                if (isProperty || isComponent)
                    fieldInfo.SetValue(this, Activator.CreateInstance(fieldType));
            }
        }
    }
}