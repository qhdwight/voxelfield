using UnityEngine;

namespace Swihoni.Util.Interface
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class InterfaceBehaviorBase : MonoBehaviour
    {
        private const float InvisibleAlpha = 0.0f, OpaqueAlpha = 1.0f;

        [SerializeField] protected bool m_NeedsCursor, m_InterruptsCommands, m_CanDeactivate;

        private bool m_HasChangedActiveState;
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

        public bool SetInterfaceActive(bool isActive)
        {
            if (m_HasChangedActiveState && IsActive == isActive) return false;
            IsActive = isActive;
            SetCanvasGroupActive(m_CanvasGroup, isActive);
            if (m_CanDeactivate)
                m_CanvasGroup.gameObject.SetActive(isActive);
            OnSetInterfaceActive(isActive);
            m_HasChangedActiveState = true;
            return true;
        }

        protected virtual void OnSetInterfaceActive(bool isActive)
        {
            
        }

        protected static void SetCanvasGroupActive(CanvasGroup group, bool isActive)
        {
            group.alpha = isActive ? OpaqueAlpha : InvisibleAlpha;
            group.interactable = isActive;
            group.blocksRaycasts = isActive;
        }
    }
}