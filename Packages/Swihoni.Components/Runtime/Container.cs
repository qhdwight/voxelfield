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
        private IDictionary<Type, ElementBase> m_Children;

        private Dictionary<Type, int> m_TypeToId = new Dictionary<Type, int>();

        public IEnumerable<Type> ElementTypes => m_Children.Keys;

        public Container()
        {
            Comparer<Type> comparer = Comparer<Type>.Create((t1, t2) => m_TypeToId[t1].CompareTo(m_TypeToId[t2]));
            m_Children = new SortedDictionary<Type, ElementBase>(comparer);
        }

        public Container(IEnumerable<Type> types) : this(types.ToArray())
        {
        }

        /// <summary>
        /// Order of types determines serialization, so server and client must have the same order.
        /// </summary>
        /// <param name="types"></param>
        public Container(params Type[] types) : this()
        {
            Add(types);
        }

        public ElementBase TryGet(Type type)
        {
            return m_TypeToId.ContainsKey(type) ? m_Children[type] : null;
        }

        public void Add(IEnumerable<Type> types)
        {
            foreach (Type type in types)
            {
                if (type.IsElement())
                {
                    if (m_TypeToId.ContainsKey(type))
                        throw new ArgumentException("Containers can only have one of each type! Make a subclass if necessary");
                    m_TypeToId[type] = m_TypeToId.Count;
                    m_Children[type] = (ElementBase) Activator.CreateInstance(type);
                }
                else
                    throw new ArgumentException("Type must be containable (Property or Component)");
            }
        }

        public void Add(params Type[] types)
        {
            Add((IEnumerable<Type>) types);
        }

        public void Set(IEnumerable<Type> types)
        {
            m_TypeToId.Clear();
            m_Children.Clear();
            Add(types);
        }

        public bool Without<TElement>(out TElement component) where TElement : ElementBase
        {
            return !If(out component);
        }
        
        public bool IfAndPreset<TElement>(out TElement component) where TElement : PropertyBase
        {
            return If(out component) && component.HasValue;
        }

        public bool If<TElement>(out TElement component) where TElement : ElementBase
        {
            bool hasComponent = m_Children.TryGetValue(typeof(TElement), out ElementBase child);
            component = (TElement) child;
            return hasComponent;
        }

        public bool Has<TElement>()
        {
            return m_Children.ContainsKey(typeof(TElement));
        }

        public TElement Require<TElement>()
        {
            try
            {
                object child = m_Children[typeof(TElement)];
                return (TElement) child;
            }
            catch (Exception exception)
            {
                throw new ArgumentException($"Container does not have {typeof(TElement).Name}", exception);
            }
        }
    }
}