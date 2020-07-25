using Swihoni.Sessions.Modes;
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

        protected override void PrimaryUse(in ModifyContext context, InventoryComponent inventory, ItemComponent item) => Swing(context, item);

        protected virtual void Swing(in ModifyContext context, ItemComponent item)
        {
            int playerId = context.playerId;
            SessionBase session = context.session;
            Ray ray = session.GetRayForPlayerId(playerId);
            session.RollbackHitboxesFor(playerId);
            int count = Physics.RaycastNonAlloc(ray, RaycastHits, m_Distance, m_RaycastMask);
            if (count == 0) return;
            
            RaycastHit hit = RaycastHits[0];
            if (!hit.collider.TryGetComponent(out PlayerHitbox hitbox) || hitbox.Manager.PlayerId == playerId) return;

            var hitContext = new PlayerHitContext(context, hitbox, this, hit);
            session.GetModifyingMode().PlayerHit(hitContext);
        }
    }
}