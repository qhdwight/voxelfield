using Swihoni.Components;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
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

        [SerializeField] private uint m_PopTimeUs = default, m_PopDurationUs = default;
        [SerializeField] private float m_Radius = default, m_Damage = default, m_Interval = default;
        [SerializeField] private LayerMask m_Mask = default;
        [SerializeField] private float m_MinimumDamageRatio = 0.2f;
        [SerializeField] private float m_CollisionVelocityMultiplier = 0.5f;

        private readonly Collider[] m_OverlappingColliders = new Collider[8];
        private uint m_LastElapsedUs;
        private CollisionType m_LastCollision;

        public Rigidbody Rigidbody { get; private set; }
        public int ThrowerId { get; set; }
        public bool PopQueued { get; set; }

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
            PopQueued = false;
            m_LastElapsedUs = 0u;
        }

        public override void Modify(SessionBase session, EntityContainer entity, uint timeUs, uint durationUs)
        {
            base.Modify(session, entity, timeUs, durationUs);

            var throwable = entity.Require<ThrowableComponent>();
            throwable.thrownElapsedUs.Value += durationUs;

            bool poppedFromTime = throwable.thrownElapsedUs >= m_PopTimeUs && m_LastElapsedUs < throwable.popTimeUs;
            if (poppedFromTime || PopQueued)
            {
                throwable.popTimeUs.Value = throwable.thrownElapsedUs;
                PopQueued = false;
            }

            bool hasPopped = throwable.thrownElapsedUs >= throwable.popTimeUs;
            Rigidbody.constraints = hasPopped ? RigidbodyConstraints.FreezeAll : RigidbodyConstraints.None;
            Transform t = transform;
            if (hasPopped)
            {
                t.rotation = Quaternion.identity;

                bool justPopped = m_LastElapsedUs < throwable.popTimeUs;

                if (m_Damage > 0 && (m_Interval > 0u || justPopped))
                    HurtNearby(session, durationUs);
            }
            else
            {
                throwable.contactElapsedUs.Value += durationUs;
                if (m_LastCollision != CollisionType.None)
                {
                    if (m_LastCollision == CollisionType.World) Rigidbody.velocity *= m_CollisionVelocityMultiplier;
                    else if (throwable.thrownElapsedUs > 100_000u) Rigidbody.velocity = Vector3.zero;
                    throwable.contactElapsedUs.Value = 0u;
                }
            }
            m_LastElapsedUs = throwable.thrownElapsedUs;
            m_LastCollision = CollisionType.None;

            throwable.position.Value = t.position;
            throwable.rotation.Value = t.rotation;

            if (throwable.popTimeUs != uint.MaxValue && throwable.thrownElapsedUs - throwable.popTimeUs > m_PopDurationUs)
                entity.Zero();
        }

        private void HurtNearby(SessionBase session, uint durationUs)
        {
            int count = Physics.OverlapSphereNonAlloc(transform.position, m_Radius, m_OverlappingColliders, m_Mask);
            for (var i = 0; i < count; i++)
            {
                Collider hitCollider = m_OverlappingColliders[i];
                if (!hitCollider.TryGetComponent(out PlayerTrigger trigger)) continue;
                int hitPlayerId = trigger.PlayerId;
                Container hitPlayer = session.GetPlayerFromId(hitPlayerId);
                if (hitPlayer.WithPropertyWithValue(out HealthProperty health) && health.IsAlive)
                {
                    byte damage = DamagePlayer(hitPlayer, durationUs);
                    session.GetMode().InflictDamage(session, ThrowerId, session.GetPlayerFromId(ThrowerId), hitPlayer, hitPlayerId, damage);
                }
            }
        }

        private byte DamagePlayer(Container hitPlayer, uint durationUs)
        {
            float distance = Vector3.Distance(hitPlayer.Require<MoveComponent>().position, transform.position);
            float ratio = (m_MinimumDamageRatio - 1.0f) * Mathf.Clamp01(distance / m_Radius) + 1.0f;
            if (m_Interval > 0u) ratio *= durationUs * TimeConversions.MicrosecondToSecond;
            return checked((byte) Mathf.Max(m_Damage * ratio, 1.0f));
        }
    }
}