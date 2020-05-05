using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityVisualBehavior : MonoBehaviour
    {
        public int id;

        [SerializeField] protected Renderer[] m_Renderers;
        protected EntityManager m_Manager;

        internal virtual void Setup(EntityManager manager) => m_Manager = manager;

        public void SetVisible(bool isEnabled)
        {
            foreach (Renderer meshRenderer in m_Renderers)
                meshRenderer.enabled = isEnabled;
        }

        public virtual void Render(EntityContainer entity)
        {
            if (entity.Without(out ThrowableComponent throwable)) return;

            SetVisible(IsVisible(entity));

            Transform t = transform;
            t.SetPositionAndRotation(throwable.position, throwable.rotation);
        }

        public virtual bool IsVisible(EntityContainer entity) => true;
    }
}