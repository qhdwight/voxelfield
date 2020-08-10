using UnityEngine;

namespace Swihoni.Util.Interface
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class InterfaceBehaviorBase : MonoBehaviour
    {
        private const float InvisibleAlpha = 0.0f, OpaqueAlpha = 1.0f;

        [SerializeField] protected bool m_NeedsCursor, m_InterruptsCommands;

        private CanvasGroup m_CanvasGroup;

        public bool IsActive { get; private set; }
        public bool NeedsCursor => IsActive && m_NeedsCursor;
        public bool InterruptsCommands => IsActive && m_InterruptsCommands;

        protected virtual void Awake()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            IsActive = m_CanvasGroup.interactable;
            // SetInterfaceActive(m_Active);
        }

        public void ToggleInterfaceActive() => SetInterfaceActive(!IsActive);

        public virtual void SetInterfaceActive(bool isActive)
        {
            if (IsActive == isActive) return;
            IsActive = isActive;
            SetCanvasGroupActive(m_CanvasGroup, isActive);
        }

        protected static void SetCanvasGroupActive(CanvasGroup group, bool isActive)
        {
            group.alpha = isActive ? OpaqueAlpha : InvisibleAlpha;
            group.interactable = isActive;
            group.blocksRaycasts = isActive;
        }
    }
}