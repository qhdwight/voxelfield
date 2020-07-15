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

        protected override void PrimaryUse(SessionBase session, int playerId, InventoryComponent inventory, ItemComponent item, uint durationUs)
        {
            Swing(session, playerId, item, durationUs);
        }

        protected virtual void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            Ray ray = session.GetRayForPlayerId(playerId);
            session.RollbackHitboxesFor(playerId);
            int count = Physics.RaycastNonAlloc(ray, RaycastHits, m_Distance, m_RaycastMask);
            if (count == 0) return;
            RaycastHit hit = RaycastHits[0];
            if (!hit.collider.TryGetComponent(out PlayerHitbox hitbox) || hitbox.Manager.PlayerId == playerId) return;
            session.GetModifyingMode().PlayerHit(session, playerId, hitbox, this, hit, durationUs);
        }
    }
}