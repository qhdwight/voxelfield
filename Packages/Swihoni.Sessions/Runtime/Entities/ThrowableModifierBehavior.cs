using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableModifierBehavior : EntityModifierBehavior
    {
        private Rigidbody m_Rigidbody;

        public Rigidbody Rigidbody => m_Rigidbody;

        private void Awake() { m_Rigidbody = GetComponent<Rigidbody>(); }

        public override void Modify(EntityContainer entity)
        {
            base.Modify(entity);

            if (entity.Without(out ThrowableComponent throwable)) return;

            Transform t = transform;
            throwable.position.Value = t.position;
            throwable.rotation.Value = t.rotation;
        }
    }
}