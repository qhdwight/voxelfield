using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityVisualBehavior : MonoBehaviour
    {
        public int id;

        private Renderer[] m_Renderers;

        internal void Setup() => m_Renderers = GetComponentsInChildren<Renderer>();

        public void SetVisible(bool isEnabled) => gameObject.SetActive(isEnabled);

        public void Render(EntityContainer entity)
        {
            if (entity.Without(out ThrowableComponent throwable)) return;
            
            Transform t = transform;
            t.SetPositionAndRotation(throwable.position, throwable.rotation);
        }
    }
}