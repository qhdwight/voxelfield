using System.Linq;
using Swihoni.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class ElementInterfaceBase<TElement> : InterfaceBehaviorBase
    {
        public abstract void Render(Container session, TElement element);
    }

    public abstract class ArrayViewerInterfaceBase<TEntry, TArray, TElement> : SessionInterfaceBehavior
        where TEntry : ElementInterfaceBase<TElement>
        where TArray : ArrayElement<TElement>
        where TElement : ElementBase, new()
    {
        [SerializeField] private GameObject m_EntryPrefab = default;
        [SerializeField] private Transform m_EntryHolder = default;

        private TEntry[] m_Entries;

        protected override void Awake()
        {
            base.Awake();
            Cleanup();
        }

        private TEntry[] GetEntries(TArray array)
        {
            if (m_Entries == null)
                m_Entries = Enumerable.Range(0, array.Length).Select(i =>
                {
                    GameObject instance = Instantiate(m_EntryPrefab, m_EntryHolder);
                    var entry = instance.GetComponent<TEntry>();
                    return entry;
                }).ToArray();
            return m_Entries;
        }

        private void SortEntries(TArray array)
        {
            TElement currentMax = null;
            for (var i = 0; i < array.Length; i++)
            {
                TElement element = array[i];
                if (currentMax != null && Less(element, currentMax)) continue;
                m_Entries[i].transform.SetAsFirstSibling();
                currentMax = element;
            }
        }

        protected abstract bool Less(TElement e1, TElement e2);

        public override void Render(SessionBase session, Container sessionContainer)
        {
            if (sessionContainer.Without(out TArray array)) return;

            TEntry[] entries = GetEntries(array);

            for (var i = 0; i < array.Length; i++)
            {
                TElement element = array[i];
                TEntry entry = entries[i];
                entry.Render(sessionContainer, element);
            }

            SortEntries(array);
        }

        public void Cleanup() => m_Entries = null;
    }
}