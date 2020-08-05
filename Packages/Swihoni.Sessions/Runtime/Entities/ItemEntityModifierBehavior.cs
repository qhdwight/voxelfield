using Swihoni.Components;
using Swihoni.Sessions.Modes;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public class ItemEntityModifierBehavior : EntityModifierBehavior
    {
        public Rigidbody Rigidbody { get; private set; }

        private void Awake() => Rigidbody = GetComponent<Rigidbody>();

        public override void SetActive(bool isActive, int index)
        {
            base.SetActive(isActive, index);
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.constraints = isActive ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
        }

        public override void Modify(in SessionContext context)
        {
            base.Modify(in context);
            
            var throwable = context.entity.Require<ThrowableComponent>();
            throwable.thrownElapsedUs.Value += context.durationUs;

            if (throwable.thrownElapsedUs > context.Mode.ItemEntityLifespanUs)
                context.entity.Clear();
        }
    }
}