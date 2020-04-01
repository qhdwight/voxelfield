using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Collections;

namespace Components
{
    public static class Extensions
    {
        private static readonly Pool<object[]> _ObjectsPool = new Pool<object[]>(10, () => new object[3]);

        private static readonly PropertyBase[] _Properties = new PropertyBase[3];
        private static readonly ArrayPropertyBase[] _Arrays = new ArrayPropertyBase[3];

        public static bool IsComponent(this Type type)
        {
            return type.IsSubclassOf(typeof(ComponentBase));
        }

        public static bool IsProperty(this Type type)
        {
            return type.IsSubclassOf(typeof(PropertyBase));
        }

        public static bool IsArrayProperty(this Type type)
        {
            return type.IsSubclassOf(typeof(ArrayPropertyBase));
        }

        internal static void Navigate(Action<FieldInfo, PropertyBase> visitProperty, object o)
        {
            object[] objects = _ObjectsPool.Obtain();
            objects[0] = o;
            Navigate((field, properties) => visitProperty(field, properties[0]), objects, 1);
            _ObjectsPool.ReturnAll();
        }

        internal static void NavigateZipped(Action<FieldInfo, PropertyBase, PropertyBase> visitProperty, object o1, object o2)
        {
            object[] objects = _ObjectsPool.Obtain();
            objects[0] = o1;
            objects[1] = o2;
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1]), objects, 2);
            _ObjectsPool.ReturnAll();
        }

        internal static void NavigateZipped(Action<FieldInfo, PropertyBase, PropertyBase, PropertyBase> visitProperty, object o1, object o2, object o3)
        {
            object[] objects = _ObjectsPool.Obtain();
            objects[0] = o1;
            objects[1] = o2;
            objects[2] = o3;
            Navigate((field, properties) => visitProperty(field, properties[0], properties[1], properties[2]), objects, 3);
            _ObjectsPool.ReturnAll();
        }

        private static void Navigate(Action<FieldInfo, PropertyBase[]> visitProperty, IReadOnlyList<object> objects, int size)
        {
            void NavigateRecursively(IReadOnlyList<object> _objects, Type _type, FieldInfo _field)
            {
                object[] mutableObjectReferences = _ObjectsPool.Obtain();
                for (var i = 0; i < size; i++)
                    if (_objects[i] == null)
                        throw new NullReferenceException("Null member");
                    else
                        mutableObjectReferences[i] = _objects[i];
                if (_type.IsComponent())
                {
                    foreach (FieldInfo field in Cache.GetFieldInfo(_type))
                    {
                        Type fieldType = field.FieldType;
                        if (fieldType.IsProperty())
                        {
                            for (var i = 0; i < size; i++)
                                _Properties[i] = (PropertyBase) field.GetValue(_objects[i]);
                            visitProperty(field, _Properties);
                        }
                        else
                        {
                            for (var i = 0; i < size; i++)
                                mutableObjectReferences[i] = field.GetValue(_objects[i]);
                            NavigateRecursively(mutableObjectReferences, fieldType, field);
                        }
                    }
                }
                else if (_type.IsArrayProperty())
                {
                    for (var i = 0; i < size; i++)
                        _Arrays[i] = (ArrayPropertyBase) _objects[i];
                    Type elementType = _Arrays.First().GetElementType();
                    bool isProperty = elementType.IsProperty();
                    for (var i = 0; i < _Arrays.First().Length; i++)
                        if (isProperty)
                        {
                            for (var j = 0; j < size; j++)
                                _Properties[j] = (PropertyBase) _Arrays[j].GetValue(i);
                            visitProperty(_field, _Properties);
                        }
                        else
                        {
                            for (var j = 0; j < size; j++)
                                mutableObjectReferences[j] = _Arrays[j].GetValue(i);
                            NavigateRecursively(mutableObjectReferences, elementType, _field);
                        }
                }
                else
                {
                    throw new Exception("Expected component or array");
                }
            }
            Type type = objects.First().GetType();
            NavigateRecursively(objects, type, null);
        }
    }
}