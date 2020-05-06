using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    [RequireComponent(typeof(Rigidbody))]
    public class ThrowableModifierBehavior : EntityModifierBehavior
    {
        [SerializeField] private float m_PopTime = default, m_Lifetime = default, m_Radius = default;
        [SerializeField] private float m_Damage;
        [SerializeField] private LayerMask m_Mask = default;

        private readonly Collider[] m_OverlappingColliders = new Collider[8];
        private float m_LastElapsed;
        private bool m_WasCollision;

        public Rigidbody Rigidbody { get; private set; }
        public int ThrowerId { get; set; }
        public float PopTime => m_PopTime;

        private void Awake() => Rigidbody = GetComponent<Rigidbody>();

        private void OnCollisionEnter() { m_WasCollision = true; }

        public override void SetActive(bool isEnabled)
        {
            base.SetActive(isEnabled);
            Rigidbody.velocity = Vector3.zero;
            Rigidbody.angularVelocity = Vector3.zero;
            Rigidbody.constraints = isEnabled ? RigidbodyConstraints.None : RigidbodyConstraints.FreezeAll;
            m_LastElapsed = 0.0f;
        }

        private readonly HashSet<PlayerHitboxManager> m_HitPlayers = new HashSet<PlayerHitboxManager>();

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
                if (m_LastElapsed < m_PopTime)
                {
                    session.RollbackHitboxesFor(ThrowerId);
                    int count = Physics.OverlapSphereNonAlloc(t.position, m_Radius, m_OverlappingColliders, m_Mask);
                    for (var i = 0; i < count; i++)
                    {
                        Collider hitCollider = m_OverlappingColliders[i];
                        var hitbox = hitCollider.GetComponent<PlayerHitbox>();
                        if (!hitbox || m_HitPlayers.Contains(hitbox.Manager)) continue;
                        m_HitPlayers.Add(hitbox.Manager);
                        int hitPlayerId = hitbox.Manager.PlayerId;
                        Container hitPlayer = session.GetPlayerFromId(hitPlayerId);
                        // TODO:feature damage based on range?
                        if (hitPlayer.Present(out HealthProperty health) && health.IsAlive)
                        {
                            var damage = checked((byte) m_Damage);
                            session.GetMode().InflictDamage(session, ThrowerId, session.GetPlayerFromId(ThrowerId), hitPlayer, hitPlayerId, damage);
                        }
                    }
                    m_HitPlayers.Clear();
                }
            }
            else
            {
                throwable.contactElapsed.Value += duration;
                if (m_WasCollision)
                {
                    throwable.contactElapsed.Value = 0.0f;
                }
            }
            m_LastElapsed = throwable.thrownElapsed;
            m_WasCollision = false;

            throwable.position.Value = t.position;
            throwable.rotation.Value = t.rotation;

            if (throwable.thrownElapsed > m_Lifetime)
                entity.Zero();
        }
    }
}