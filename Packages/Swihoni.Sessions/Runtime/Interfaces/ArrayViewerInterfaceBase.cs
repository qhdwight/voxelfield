using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Swihoni.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class ElementInterfaceBase<TElement> : InterfaceBehaviorBase
    {
        public abstract void Render(in SessionContext context, TElement element);
    }

    public abstract class ArrayViewerInterfaceBase<TEntry, TArray, TElement> : SessionInterfaceBehavior
        where TEntry : ElementInterfaceBase<TElement>
        where TArray : ArrayElement<TElement>
        where TElement : ElementBase, new()
    {
        [SerializeField] private GameObject m_EntryPrefab = default;
        [SerializeField] private Transform m_EntryHolder = default;

        private TEntry[] m_Entries;
        private readonly Comparer<TElement> m_Comparer;
        
        protected ArrayViewerInterfaceBase() => m_Comparer = Comparer<TElement>.Create(Compare);

        protected override void Awake()
        {
            base.Awake();
            Cleanup();
        }

        private TEntry[] GetEntries(TArray array)
        {
            return m_Entries ?? (m_Entries = Enumerable.Range(0, array.Length).Select(i =>
            {
                GameObject instance = Instantiate(m_EntryPrefab, m_EntryHolder);
                var entry = instance.GetComponent<TEntry>();
                return entry;
            }).ToArray());
        }
        
        protected abstract int Compare(TElement e1, TElement e2);

        public override void Render(in SessionContext context)
        {
            if (context.sessionContainer.Without(out TArray array)) return;

            TEntry[] entries = GetEntries(array);

            var i = 0;
            foreach (TElement element in array.ToImmutableSortedSet(m_Comparer))
                entries[array.Length - ++i].Render(context, element);
        }

        private void Cleanup() => m_Entries = null;
    }
}