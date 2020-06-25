using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityVisualBehavior : VisualBehaviorBase
    {
        public virtual void Render(Container entity)
        {
            if (entity.Without(out ThrowableComponent throwable)) return;
            SetVisible(IsVisible(entity));
            Transform t = transform;
            t.SetPositionAndRotation(throwable.position, throwable.rotation);
        }

        public virtual bool IsVisible(Container entity) => true;
    }
}