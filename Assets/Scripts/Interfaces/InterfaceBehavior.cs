using UnityEngine;

namespace Interfaces
{
    [RequireComponent(typeof(CanvasGroup))]
    public class InterfaceBehavior : MonoBehaviour
    {
        protected const float INVISIBLE_ALPHA = 0.0f, OPAQUE_ALPHA = 1.0f;

        private CanvasGroup m_CanvasGroup;

        public bool IsActive { get; private set; }

        protected virtual void Awake()
        {
            m_CanvasGroup = GetComponent<CanvasGroup>();
            IsActive      = m_CanvasGroup.interactable;
            // SetInterfaceActive(m_Active);
        }

        public void ToggleInterfaceActive()
        {
            SetInterfaceActive(!IsActive);
        }

        public virtual void SetInterfaceActive(bool active)
        {
            if (IsActive == active) return;
            IsActive                     = active;
            m_CanvasGroup.alpha          = active ? OPAQUE_ALPHA : INVISIBLE_ALPHA;
            m_CanvasGroup.interactable   = active;
            m_CanvasGroup.blocksRaycasts = active;
        }
    }
}