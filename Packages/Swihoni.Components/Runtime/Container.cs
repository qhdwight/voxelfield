using System;
using System.Collections.Generic;
using System.Linq;

namespace Swihoni.Components
{
    [Serializable]
    public class Container : ComponentBase
    {
        private Dictionary<Type, int> m_TypeToId = new Dictionary<Type, int>();

        public IEnumerable<Type> ElementTypes => m_TypeToId.Keys;

        public Container() { }

        public Container(IEnumerable<Type> types) : this(types.ToArray()) { }

        /// <summary>
        /// Order of types determines serialization, so server and client must have the same order.
        /// </summary>
        /// <param name="types"></param>
        public Container(params Type[] types) => Add(types);

        protected override void Register(ElementBase instance)
        {
            base.Register(instance);
            Type type = instance.GetType();
            if (m_TypeToId.ContainsKey(type))
                throw new ArgumentException("Containers can only have one of each type! Make a subclass if necessary");
            m_TypeToId[type] = m_TypeToId.Count;
        }

        public ElementBase TryGet(Type type) => m_TypeToId.TryGetValue(type, out int index) ? Elements[index] : null;

        public void Add(IEnumerable<Type> types)
        {
            foreach (Type type in types)
                Add(type);
        }

        private void Add(Type type)
        {
            if (!type.IsElement())
                throw new ArgumentException("Type must be containable (Property or Component)");
            Register((ElementBase) Activator.CreateInstance(type));
        }

        public void Add(params Type[] types) => Add((IEnumerable<Type>) types);

        public void TakeElementTypes(Container other)
        {
            m_TypeToId.Clear();
            ClearRegistered();
            foreach (ElementBase element in other.Elements)
                Add(element.GetType());
        }

        public bool Without<TElement>() where TElement : ElementBase { return !Has<TElement>(); }

        public bool Without<TElement>(out TElement component) where TElement : ElementBase { return !Has(out component); }

        public bool Present<TElement>(out TElement component) where TElement : PropertyBase { return Has(out component) && component.HasValue; }

        public bool Has<TElement>(out TElement component) where TElement : ElementBase
        {
            bool hasComponent = m_TypeToId.TryGetValue(typeof(TElement), out int index);
            if (hasComponent)
            {
                component = (TElement) Elements[index];
                return true;
            }
            component = null;
            return false;
        }

        public void ZeroIfHas<TElement>() where TElement : ElementBase
        {
            if (Has(out TElement element)) element.Zero();
        }

        public bool Has<TElement>() => m_TypeToId.ContainsKey(typeof(TElement));

        public TElement Require<TElement>()
        {
            try
            {
                object child = Elements[m_TypeToId[typeof(TElement)]];
                return (TElement) child;
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Container does not have {typeof(TElement).Name}", exception);
            }
        }
    }
}