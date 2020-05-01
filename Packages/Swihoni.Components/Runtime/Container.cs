using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Swihoni.Components
{
    [Serializable]
    public class Container : ComponentBase
    {
        [DebuggerBrowsable(DebuggerBrowsableState.RootHidden)]
        private List<ElementBase> m_Elements;

        private Dictionary<Type, int> m_TypeToId = new Dictionary<Type, int>();

        public IEnumerable<Type> ElementTypes => m_TypeToId.Keys;

        public IReadOnlyList<ElementBase> Elements => m_Elements;

        public Container() : this(0) { }

        public Container(int capacity) { m_Elements = new List<ElementBase>(capacity); }

        public Container(IEnumerable<Type> types) : this(types.ToArray()) { }

        /// <summary>
        /// Order of types determines serialization, so server and client must have the same order.
        /// </summary>
        /// <param name="types"></param>
        public Container(params Type[] types) : this(types.Length) { Add(types); }

        public ElementBase TryGet(Type type) { return m_TypeToId.TryGetValue(type, out int index) ? m_Elements[index] : null; }

        public void Add(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                if (type.IsElement())
                {
                    if (m_TypeToId.ContainsKey(type))
                        throw new ArgumentException("Containers can only have one of each type! Make a subclass if necessary");
                    m_TypeToId[type] = m_TypeToId.Count;
                    m_Elements.Add((ElementBase) Activator.CreateInstance(type));
                }
                else
                    throw new ArgumentException("Type must be containable (Property or Component)");
            }
        }

        public void Add(params Type[] types) { Add((IEnumerable<Type>) types); }

        public void Set(IEnumerable<Type> types)
        {
            m_TypeToId.Clear();
            m_Elements.Clear();
            Add(types);
        }

        public bool Without<TElement>() where TElement : ElementBase { return !Has<TElement>(); }

        public bool Without<TElement>(out TElement component) where TElement : ElementBase { return !Has(out component); }

        public bool Present<TElement>(out TElement component) where TElement : PropertyBase { return Has(out component) && component.HasValue; }

        public bool Has<TElement>(out TElement component) where TElement : ElementBase
        {
            bool hasComponent = m_TypeToId.TryGetValue(typeof(TElement), out int index);
            if (hasComponent)
            {
                component = (TElement) m_Elements[index];
                return true;
            }
            component = null;
            return false;
        }

        public bool Has<TElement>() { return m_TypeToId.ContainsKey(typeof(TElement)); }

        public TElement Require<TElement>()
        {
            try
            {
                object child = m_Elements[m_TypeToId[typeof(TElement)]];
                return (TElement) child;
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Container does not have {typeof(TElement).Name}", exception);
            }
        }
    }
}