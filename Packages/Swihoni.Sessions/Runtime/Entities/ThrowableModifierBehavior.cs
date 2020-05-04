using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableModifierBehavior : EntityModifierBehavior
    {
        [SerializeField] private float m_ExplodeTime, m_Lifetime;

        public Rigidbody Rigidbody { get; private set; }

        private void Awake() => Rigidbody = GetComponent<Rigidbody>();

        public override void SetActive(bool isEnabled)
        {
            base.SetActive(isEnabled);
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
        }

        public override void Modify(EntityContainer entity, float duration)
        {
            base.Modify(entity, duration);

            if (entity.Without(out ThrowableComponent throwable)) return;

            Transform t = transform;
            throwable.position.Value = t.position;
            throwable.rotation.Value = t.rotation;

            throwable.elapsed.Value += duration;
            if (throwable.elapsed > m_Lifetime)
                entity.Zero();
        }
    }
}