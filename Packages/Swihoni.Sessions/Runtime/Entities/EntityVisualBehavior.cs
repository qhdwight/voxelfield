using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityVisualBehavior : VisualBehaviorBase
    {
        protected IBehaviorManager m_Manager;

        internal override void Setup(IBehaviorManager manager)
        {
            base.Setup(manager);
            m_Manager = manager;
        }

        public override void Render(Container entity)
        {
            if (entity.Without(out ThrowableComponent throwable)) return;
            SetVisible(IsVisible(entity));
            Transform t = transform;
            t.SetPositionAndRotation(throwable.position, throwable.rotation);
        }

        public virtual bool IsVisible(Container entity) => true;
    }
}