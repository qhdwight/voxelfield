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
        [SerializeField] protected bool m_IsSticky, m_ExplodeOnContact;

        private RigidbodyConstraints m_InitialConstraints;
        private readonly Collider[] m_OverlappingColliders = new Collider[8];
        private uint m_LastElapsedUs;
        private bool m_IsFrozen;
        private (CollisionType, Collision) m_LastCollision;

        public string Name { get; set; }
        public Rigidbody Rigidbody { get; private set; }
        public int ThrowerId { get; set; }
        public bool PopQueued { get; set; }
        public bool CanQueuePop => m_PopTimeUs == uint.MaxValue;
        public float Radius => m_Radius;

        private void Awake()
        {
            Rigidbody = GetComponent<Rigidbody>();
            m_InitialConstraints = Rigidbody.constraints;
        }

        private void OnCollisionEnter(Collision collision)
        {
            if (m_IsFrozen) return;
            bool isInMask = (m_Mask & (1 << collision.gameObject.layer)) != 0;
            m_LastCollision = (isInMask ? CollisionType.Player : CollisionType.World, collision);
        }

        private void ResetRigidbody(bool canMove)
        {
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.constraints = canMove ? m_InitialConstraints : RigidbodyConstraints.FreezeAll;
        }

        public override void SetActive(bool isActive)
        {
            base.SetActive(isActive);
            ResetRigidbody(isActive);
            PopQueued = false;
            m_LastElapsedUs = 0u;
            m_LastCollision = (CollisionType.None, null);
            m_IsFrozen = false;
        }

        private static Vector3 GetSurfaceNormal(Collision collision)
        {
            Vector3 point = collision.contacts[0].point,
                    direction = collision.contacts[0].normal;
            point += direction;
            return collision.collider.Raycast(new Ray(point, -direction), out RaycastHit hit, 2.0f)
                ? hit.normal
                : Vector3.up;
        }

        public override void Modify(SessionBase session, Container container, uint timeUs, uint durationUs)
        {
            base.Modify(session, container, timeUs, durationUs);

            var throwable = container.Require<ThrowableComponent>();
            throwable.thrownElapsedUs.Value += durationUs;

            bool poppedFromTime = throwable.thrownElapsedUs >= m_PopTimeUs && m_LastElapsedUs < throwable.popTimeUs;
            if (poppedFromTime || PopQueued)
            {
                throwable.popTimeUs.Value = throwable.thrownElapsedUs;
                PopQueued = false;
            }

            bool hasPopped = throwable.thrownElapsedUs >= throwable.popTimeUs;
            if (hasPopped) m_IsFrozen = true;
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
                (CollisionType collisionType, Collision collision) = m_LastCollision;
                if (collisionType != CollisionType.None)
                {
                    var resetContact = true;
                    if (m_ExplodeOnContact)
                    {
                        throwable.popTimeUs.Value = throwable.thrownElapsedUs;
                        resetContact = false;
                        HurtNearby(session, durationUs);
                    }
                    if (collisionType == CollisionType.World && !m_ExplodeOnContact)
                    {
                        if (m_IsSticky)
                        {
                            ResetRigidbody(false);
                            Vector3 surfaceNormal = GetSurfaceNormal(collision);
                            Rigidbody.transform.SetPositionAndRotation(collision.contacts[0].point,
                                                                       Quaternion.FromToRotation(Rigidbody.transform.up, surfaceNormal) * Rigidbody.rotation);
                            m_IsFrozen = true;
                        }
                        else
                        {
                            Rigidbody.velocity *= m_CollisionVelocityMultiplier;
                        }
                    }
                    else if (throwable.thrownElapsedUs > 100_000u) Rigidbody.velocity = Vector3.zero; // Stop on hitting player. Delay to prevent hitting self
                    if (resetContact) throwable.contactElapsedUs.Value = 0u;
                }
            }
            m_LastElapsedUs = throwable.thrownElapsedUs;
            m_LastCollision = (CollisionType.None, null);
            Rigidbody.constraints = m_IsFrozen ? RigidbodyConstraints.FreezeAll : m_InitialConstraints;

            throwable.position.Value = t.position;
            throwable.rotation.Value = t.rotation;

            if (throwable.popTimeUs != uint.MaxValue && throwable.thrownElapsedUs - throwable.popTimeUs > m_PopDurationUs)
                container.Zero();
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
                    byte damage = CalculateDamage(hitPlayer, durationUs);
                    session.GetMode().InflictDamage(session, ThrowerId, session.GetPlayerFromId(ThrowerId), hitPlayer, hitPlayerId, damage, Name);
                }
            }
            session.Injector.OnThrowablePopped(this);
        }

        private byte CalculateDamage(Container hitPlayer, uint durationUs)
        {
            float distance = Vector3.Distance(hitPlayer.Require<MoveComponent>().position, transform.position);
            float ratio = (m_MinimumDamageRatio - 1.0f) * Mathf.Clamp01(distance / m_Radius) + 1.0f;
            if (m_Interval > 0u) ratio *= durationUs * TimeConversions.MicrosecondToSecond;
            return checked((byte) Mathf.Max(m_Damage * ratio, 1.0f));
        }
    }
}