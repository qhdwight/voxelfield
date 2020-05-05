using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableModifierBehavior : EntityModifierBehavior
    {
        [SerializeField] private float m_PopTime, m_Lifetime, m_Radius;
        [SerializeField] private LayerMask m_Mask;

        private readonly Collider[] m_OverlappingColliders = new Collider[8];
        private float m_LastElapsed;

        public Rigidbody Rigidbody { get; private set; }
        public float PopTime => m_PopTime;

        private void Awake() => Rigidbody = GetComponent<Rigidbody>();

        public override void SetActive(bool isEnabled)
        {
            base.SetActive(isEnabled);
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.constraints = isEnabled ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
            m_LastElapsed = 0.0f;
        }

        public override void Modify(EntityContainer entity, float duration)
        {
            base.Modify(entity, duration);

            var throwable = entity.Require<ThrowableComponent>();
            throwable.elapsed.Value += duration;
            
            bool hasPopped = throwable.elapsed > m_PopTime;
            Rigidbody.constraints = hasPopped ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
            Transform t = transform;
            if (hasPopped)
            {
                t.rotation = Quaternion.identity;
                if (m_LastElapsed < m_PopTime)
                {
                    Physics.OverlapSphereNonAlloc(t.position, m_Radius, m_OverlappingColliders, m_Mask.value);
                }
            }
            m_LastElapsed = throwable.elapsed;
            
            throwable.position.Value = t.position;
            throwable.rotation.Value = t.rotation;
            
            if (throwable.elapsed > m_Lifetime)
                entity.Zero();
        }
    }
}