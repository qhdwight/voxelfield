using System;
using System.Collections.Generic;

namespace Swihoni.Components
{
    /// <summary>
    /// Allows elements to be referenced by type.
    /// Fields come first in elements, and then dynamically added elements.
    /// </summary>
    [Serializable]
    public class Container : ComponentBase, IEnumerable<(Type, ElementBase)>
    {
        private Dictionary<Type, int> m_TypeToIndex = new();

        public IEnumerable<Type> ElementTypes => m_TypeToIndex.Keys;

        public Container() { }

        public Container(IEnumerable<Type> types) => RegisterAppend(types);

        /// <summary>
        /// Order of types determines serialization, so server and client must have the same order.
        /// </summary>
        public Container(params Type[] types) : this((IEnumerable<Type>) types) { }

        public Container(IEnumerable<ElementBase> elements) => Append(elements);

        public Container(params ElementBase[] elements) : this((IEnumerable<ElementBase>) elements) { }

        public override int Append(ElementBase element)
        {
            int index = base.Append(element);
            Type type = element.GetType();
            if (m_TypeToIndex.ContainsKey(type))
                throw new ArgumentException("Containers can only have one of each type! Make a subclass if necessary");
            m_TypeToIndex[type] = m_TypeToIndex.Count;
            return index;
        }

        public void Append(IEnumerable<ElementBase> elements)
        {
            foreach (ElementBase element in elements) Append(element);
        }

        public ElementBase TryGet(Type type) => m_TypeToIndex.TryGetValue(type, out int index) ? Elements[index] : null;

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
            try
            {
                Append(type.NewElement());
            }
            catch (Exception)
            {
                throw new ArgumentException("Type must be containable (An element, for example: property or component)");
            }
        }

        /// <summary><see cref="RegisterAppend(System.Collections.Generic.IEnumerable{System.Type})"/></summary>
        public void RegisterAppend(params Type[] types) => RegisterAppend((IEnumerable<Type>) types);

        /// <summary>
        /// Clears all elements and sets types to match given container.
        /// Note: Use carefully as this unregisters field elements.
        /// TODO: only allow on containers of same type
        /// </summary>
        public virtual void TakeElementTypes(Container other)
        {
            VerifyFieldsRegistered();
            foreach (ElementBase element in other.Elements)
                if (!m_TypeToIndex.ContainsKey(element.GetType()))
                    RegisterAppend(element.GetType());
        }

        public bool Without<TElement>() where TElement : ElementBase => !With<TElement>();

        public bool Without<TElement>(out TElement component) where TElement : ElementBase => !With(out component);

        public bool WithPropertyWithValue<TElement>(out TElement component) where TElement : PropertyBase
            => With(out component) && component.WithValue;

        public bool WithoutPropertyOrWithoutValue<TElement>(out TElement component) where TElement : PropertyBase
            => Without(out component) || component.WithoutValue;

        public bool With<TElement>(out TElement component) where TElement : ElementBase
        {
            bool withComponent = m_TypeToIndex.TryGetValue(typeof(TElement), out int index);
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

        public void ClearIfWith<TElement>() where TElement : ElementBase
        {
            if (With(out TElement element)) element.Clear();
        }

        public bool With<TElement>() => m_TypeToIndex.ContainsKey(typeof(TElement));

        public TElement Require<TElement>()
        {
            try
            {
                object child = Elements[m_TypeToIndex[typeof(TElement)]];
                return (TElement) child;
            }
            catch (KeyNotFoundException)
            {
                throw new ArgumentException($"Container does not have {typeof(TElement).Name}");
            }
        }

        public new IEnumerator<(Type, ElementBase)> GetEnumerator()
        {
            foreach (ElementBase element in Elements)
                yield return (element.GetType(), element);
        }
    }
}