using System;
using System.Collections.Generic;
using System.Linq;

namespace Swihoni.Components
{
    /// <summary>
    /// Allows elements to be referenced by type.
    /// Fields come first in elements, and then dynamically added elements.
    /// </summary>
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
        public Container(params Type[] types) => RegisterAppend(types);

        protected override int Append(ElementBase element)
        {
            int index = base.Append(element);
            Type type = element.GetType();
            if (m_TypeToId.ContainsKey(type))
                throw new ArgumentException("Containers can only have one of each type! Make a subclass if necessary");
            m_TypeToId[type] = m_TypeToId.Count;
            return index;
        }

        public ElementBase TryGet(Type type) => m_TypeToId.TryGetValue(type, out int index) ? Elements[index] : null;

        /// <summary><see cref="RegisterAppend(System.Collections.Generic.IEnumerable{System.Type})"/></summary>
        public void RegisterAppend(IEnumerable<Type> types)
        {
            foreach (Type type in types) RegisterAppend(type);
        }

        /// <summary>
        /// Append an element type. Since elements must have zero argument constructors, an instance will be automatically created and registered.
        /// </summary>
        /// <param name="type">Must be an element.</param>
        /// <exception cref="ArgumentException"></exception>
        private void RegisterAppend(Type type)
        {
            if (!type.IsElement())
                throw new ArgumentException("Type must be containable (An element, for example: property or component)");
            Append((ElementBase) Activator.CreateInstance(type));
        }

        /// <summary><see cref="RegisterAppend(System.Collections.Generic.IEnumerable{System.Type})"/></summary>
        public void RegisterAppend(params Type[] types) => RegisterAppend((IEnumerable<Type>) types);

        /// <summary>
        /// Clears all elements and sets types to match given container.
        /// Note: Use carefully as this unregisters field elements.
        /// TODO: only allow on containers of same type
        /// </summary>
        public void TakeElementTypes(Container other)
        {
            m_TypeToId.Clear();
            ClearRegistered();
            foreach (ElementBase element in other.Elements)
                RegisterAppend(element.GetType());
        }

        public bool Without<TElement>() where TElement : ElementBase => !With<TElement>();

        public bool Without<TElement>(out TElement component) where TElement : ElementBase => !With(out component);

        public bool WithPropertyWithValue<TElement>(out TElement component) where TElement : PropertyBase => With(out component) && component.WithValue;

        public bool With<TElement>(out TElement component) where TElement : ElementBase
        {
            bool withComponent = m_TypeToId.TryGetValue(typeof(TElement), out int index);
            if (withComponent)
            {
                component = (TElement) Elements[index];
                return true;
            }
            component = null;
            return false;
        }

        public void ZeroIfWith<TElement>() where TElement : ElementBase
        {
            if (With(out TElement element)) element.Zero();
        }

        public bool With<TElement>() => m_TypeToId.ContainsKey(typeof(TElement));

        public TElement Require<TElement>()
        {
            try
            {
                object child = Elements[m_TypeToId[typeof(TElement)]];
                return (TElement) child;
            }
            catch (KeyNotFoundException exception)
            {
                throw new ArgumentException($"Container does not have {typeof(TElement).Name}", exception);
            }
        }
    }
}