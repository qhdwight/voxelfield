using UnityEngine;

namespace Swihoni.Util.Interface
{
    [RequireComponent(typeof(CanvasGroup))]
    public abstract class InterfaceBehaviorBase : MonoBehaviour
    {
        private const float InvisibleAlpha = 0.0f, OpaqueAlpha = 1.0f;

        private CanvasGroup m_CanvasGroup;

        public bool IsActive { get; private set; }

        protected virtual void Awake()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            IsActive = m_CanvasGroup.interactable;
            // SetInterfaceActive(m_Active);
        }

        public void ToggleInterfaceActive() { SetInterfaceActive(!IsActive); }

        public virtual void SetInterfaceActive(bool active)
        {
            if (IsActive == active) return;
            IsActive = active;
            m_CanvasGroup.alpha = active ? OpaqueAlpha : InvisibleAlpha;
            m_CanvasGroup.interactable = active;
            m_CanvasGroup.blocksRaycasts = active;
        }
    }
}