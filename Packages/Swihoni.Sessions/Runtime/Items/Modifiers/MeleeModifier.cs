using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    [CreateAssetMenu(fileName = "Melee", menuName = "Item/Melee", order = 0)]
    public class MeleeModifier : WeaponModifierBase
    {
        protected static readonly RaycastHit[] RaycastHits = new RaycastHit[1];

        [SerializeField] private float m_Distance = 2.0f;

        protected override void PrimaryUse(in SessionContext context, InventoryComponent inventory, ItemComponent item) => Swing(context, item);

        protected virtual void Swing(in SessionContext context, ItemComponent item)
        {
            int playerId = context.playerId;
            SessionBase session = context.session;
            Ray ray = session.GetRayForPlayerId(playerId);
            session.RollbackHitboxesFor(context);
            int count = context.PhysicsScene.Raycast(ray, RaycastHits, m_Distance, m_RaycastMask);
            if (!RaycastHits.TryClosest(count, out RaycastHit hit)) return;
            if (!hit.collider.TryGetComponent(out PlayerHitbox hitbox) || hitbox.Manager.PlayerId == playerId) return;

            var hitContext = new PlayerHitContext(context, hitbox, this, hit);
            context.ModifyingMode.PlayerHit(hitContext);
        }
    }
}