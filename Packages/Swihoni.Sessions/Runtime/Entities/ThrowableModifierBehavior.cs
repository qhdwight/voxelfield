using Swihoni.Components;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableModifierBehavior : EntityModifierBehavior
    {
        private enum CollisionType
        {
            None,
            World,
            Player
        }

        [SerializeField] private float m_PopTime = default, m_Lifetime = default, m_Radius = default, m_Damage = default, m_Interval = default;
        [SerializeField] private LayerMask m_Mask = default;
        [SerializeField] private float m_CollisionVelocityMultiplier = 0.5f;

        private readonly Collider[] m_OverlappingColliders = new Collider[8];
        private float m_LastElapsed;
        private CollisionType m_LastCollision;

        public Rigidbody Rigidbody { get; private set; }
        public int ThrowerId { get; set; }
        public float PopTime => m_PopTime;

        private void Awake() => Rigidbody = GetComponent<Rigidbody>();

        private void OnCollisionEnter(Collision other)
        {
            bool isInMask = (m_Mask & (1 << other.gameObject.layer)) != 0;
            m_LastCollision = isInMask ? CollisionType.Player : CollisionType.World;
        }

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
            throwable.thrownElapsed.Value += duration;

            bool hasPopped = throwable.thrownElapsed > m_PopTime;
            Rigidbody.constraints = hasPopped ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
            Transform t = transform;
            if (hasPopped)
            {
                t.rotation = Quaternion.identity;
                bool justPopped = m_LastElapsed < m_PopTime;
                if (m_Damage > 0 && (m_Interval > Mathf.Epsilon || justPopped))
                    HurtNearby(session, duration);
            }
            else
            {
                throwable.contactElapsed.Value += duration;
                if (m_LastCollision != CollisionType.None)
                {
                    if (m_LastCollision == CollisionType.World) Rigidbody.velocity *= m_CollisionVelocityMultiplier;
                    else if (throwable.thrownElapsed > 0.1f) Rigidbody.velocity = Vector3.zero;
                    throwable.contactElapsed.Value = 0.0f;
                }
            }
            m_LastElapsed = throwable.thrownElapsed;
            m_LastCollision = CollisionType.None;

            throwable.position.Value = t.position;
            throwable.rotation.Value = t.rotation;

            if (throwable.thrownElapsed > m_Lifetime)
                entity.Zero();
        }

        private void HurtNearby(SessionBase session, float duration)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, m_Radius, m_OverlappingColliders, m_Mask);
            for (var i = 0; i < count; i++)
            {
                Collider hitCollider = m_OverlappingColliders[i];
                var trigger = hitCollider.GetComponent<PlayerTrigger>();
                if (!trigger) continue;
                int hitPlayerId = trigger.PlayerId;
                Container hitPlayer = session.GetPlayerFromId(hitPlayerId);
                // TODO:feature damage based on range?
                if (hitPlayer.Present(out HealthProperty health) && health.IsAlive)
                {
                    byte damage = DamagePlayer(hitPlayer, duration);
                    session.GetMode().InflictDamage(session, ThrowerId, session.GetPlayerFromId(ThrowerId), hitPlayer, hitPlayerId, damage);
                }
            }
        }

        private byte DamagePlayer(Container hitPlayer, float duration)
        {
            const float minRatio = 0.2f;
            float distance = Vector3.Distance(hitPlayer.Require<MoveComponent>().position, transform.position);
            float ratio = (minRatio - 1.0f) * Mathf.Clamp01(distance / m_Radius) + 1.0f;
            if (m_Interval > Mathf.Epsilon) ratio *= duration;
            return checked((byte) Mathf.Max(m_Damage * ratio, 1.0f));
        }
    }
}