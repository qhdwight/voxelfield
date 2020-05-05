using Swihoni.Components;
using Swihoni.Sessions.Player;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableModifierBehavior : EntityModifierBehavior
    {
        [SerializeField] private float m_PopTime = default, m_Lifetime = default, m_Radius = default;
        [SerializeField] private LayerMask m_Mask = default;

        private readonly Collider[] m_OverlappingColliders = new Collider[8];
        private float m_LastElapsed;

        public Rigidbody Rigidbody { get; private set; }
        public int ThrowerId { get; set; }
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

        public override void Modify(SessionBase session, EntityContainer entity, float duration)
        {
            base.Modify(session, entity, duration);

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
                    session.RollbackHitboxesFor(ThrowerId);
                    int count = Physics.OverlapSphereNonAlloc(t.position, m_Radius, m_OverlappingColliders, m_Mask);
                    for (var i = 0; i < count; i++)
                    {
                        Collider hitCollider = m_OverlappingColliders[i];
                        var hitbox = hitCollider.GetComponent<PlayerHitbox>();
                        if (hitbox)
                        {
                            int hitPlayerId = hitbox.Manager.PlayerId;
                            session.GetMode().KillPlayer(session.GetPlayerFromId(hitPlayerId));
                        }
                    }
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