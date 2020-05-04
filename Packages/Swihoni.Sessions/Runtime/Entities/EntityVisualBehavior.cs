using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityVisualBehavior : MonoBehaviour
    {
        public int id;

        private Renderer[] m_Renderers;

        internal void Setup() => m_Renderers = GetComponentsInChildren<Renderer>();

        public void SetVisible(bool isEnabled) => gameObject.SetActive(isEnabled);
    }
}