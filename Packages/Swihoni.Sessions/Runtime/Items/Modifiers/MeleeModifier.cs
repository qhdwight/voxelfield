using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Melee", menuName = "Item/Melee", order = 0)]
    public class MeleeModifier : WeaponModifierBase
    {
        private static readonly RaycastHit[] RaycastHits = new RaycastHit[1];

        [SerializeField] private float m_Distance = 2.0f;

        protected override void PrimaryUse(SessionBase session, int playerId, ItemComponent item, float duration) { Swing(session, playerId, item, duration); }

        protected virtual void Swing(SessionBase session, int playerId, ItemComponent item, float duration)
        {
            Ray ray = session.GetRayForPlayerId(playerId);
            session.RollbackHitboxesFor(playerId);
            int count = Physics.RaycastNonAlloc(ray, RaycastHits, m_Distance, m_PlayerMask);
            if (count == 0) return;
            RaycastHit hit = RaycastHits[0];
            var hitbox = hit.collider.GetComponent<PlayerHitbox>();
            if (!hitbox || hitbox.Manager.PlayerId == playerId) return;
            session.GetMode().PlayerHit(session, playerId, hitbox, this, hit, duration);
        }
    }
}