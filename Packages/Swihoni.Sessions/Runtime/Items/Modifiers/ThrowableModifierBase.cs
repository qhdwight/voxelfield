using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    public static class ThrowableStatusId
    {
        public const byte Cooking = ItemStatusId.Last + 1;
    }

    public class ThrowableModifierBase : ItemModifierBase
    {
        [Header("Throwable"), SerializeField] private ushort m_ThrowForce = default;
        [SerializeField] protected byte m_AmmoCount = default;
        [SerializeField] protected GameObject m_ThrowablePrefab = default;

        public byte AmmoCount => m_AmmoCount;
        public ushort ThrowForce => m_ThrowForce;
        public GameObject ThrowablePrefab => m_ThrowablePrefab;

        protected override void StatusTick(SessionBase session, int playerId, ItemComponent item, InputFlagProperty inputs, float duration)
        {
            if (!inputs.GetInput(PlayerInput.UseOne) && item.status.id == ThrowableStatusId.Cooking)
                StartStatus(session, playerId, item, ItemStatusId.PrimaryUsing, duration);
        }

        protected override byte? FinishStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            switch (item.status.id)
            {
                case ThrowableStatusId.Cooking:
                    Release(session, playerId);
                    return ItemStatusId.Idle;
                case ItemStatusId.PrimaryUsing:
                    Release(session, playerId);
                    break;
            }
            return base.FinishStatus(session, playerId, item, inventory, inputs);
        }

        protected override byte GetUseStatus(InputFlagProperty inputProperty) => ThrowableStatusId.Cooking;

        protected override bool CanUse(ItemComponent item, InventoryComponent inventory, bool justFinishedUse = false) =>
            base.CanUse(item, inventory, justFinishedUse) && item.status.id != ThrowableStatusId.Cooking;

        protected override void PrimaryUse(SessionBase session, int playerId, ItemComponent item, float duration) { }
        
        
        protected virtual void Release(SessionBase session, int playerId)
        {
            Container player = session.GetPlayerFromId(playerId);
            Ray ray = SessionBase.GetRayForPlayer(player);
            if (player.Has<ServerTag>())
            {
                // var projectile = ProjectileManager.Singleton.InstantiateProjectile<ThrowableProjectile>(m_Properties.ThrowablePrefab);
                // projectile.transform.SetPositionAndRotation(ray.origin + ray.direction, Quaternion.identity);
                // projectile.RigidBody.AddForce(ray.direction * m_ThrowForce, ForceMode.Impulse);
                // projectile.RigidBody.AddRelativeTorque(new Vector3 {y = 2.0f}, ForceMode.Impulse);
            }
        }
    }
}