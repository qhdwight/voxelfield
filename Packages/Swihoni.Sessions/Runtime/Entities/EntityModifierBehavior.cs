using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityModifierBehavior : ModifierBehaviorBase
    {
        public virtual void Modify(in SessionContext context)
        {
            if (context.entity.Without(out ThrowableComponent throwable)) return;
            
            Transform t = transform;
            throwable.position.Value = t.position;
            throwable.rotation.Value = t.rotation;
        }
    }
}