using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
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
        [Header("Throwable"), SerializeField] private float m_ThrowForce = default;
        [SerializeField] protected ThrowableModifierBehavior m_ThrowablePrefab = default;

        protected override void StatusTick(SessionBase session, int playerId, InventoryComponent inventory, ItemComponent item, InputFlagProperty inputs, uint durationUs)
        {
            if (item.status.id == ThrowableStatusId.Cooking && !inputs.GetInput(PlayerInput.UseOne))
                StartStatus(session, playerId, inventory, item, ItemStatusId.PrimaryUsing, durationUs);
        }

        protected override byte? FinishStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            // TODO:refactor make base class for C4 type objects
            if (item.status.id == ItemStatusId.PrimaryUsing)
            {
                Release(session, playerId, item);
                if (item.ammoInReserve == 0 && item.id != ItemId.C4) return ItemStatusId.RequestRemoval;
            }
            else if (item.status.id == ItemStatusId.SecondaryUsing && item.id == ItemId.C4 && item.ammoInReserve == 0)
                return ItemStatusId.RequestRemoval;
            return base.FinishStatus(session, playerId, item, inventory, inputs);
        }

        protected override byte GetUseStatus(InputFlagProperty input) => ThrowableStatusId.Cooking;

        protected override bool CanPrimaryUse(ItemComponent item, InventoryComponent inventory, bool justFinishedUse = false)
            => base.CanPrimaryUse(item, inventory, justFinishedUse) && item.status.id != ThrowableStatusId.Cooking && item.ammoInReserve > 0;

        protected override bool CanSecondaryUse(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory)
            => base.CanPrimaryUse(item, inventory) && item.status.id != ThrowableStatusId.Cooking;

        protected override void PrimaryUse(SessionBase session, int playerId, InventoryComponent inventory, ItemComponent item, uint durationUs) { }

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            var entities = session.GetLatestSession().Require<EntityArrayElement>();
            for (var index = 0; index < entities.Length; index++)
            {
                EntityContainer entity = entities[index];
                if (session.EntityManager.UnsafeModifiers[index] is ThrowableModifierBehavior throwableModifier
                 && throwableModifier.CanQueuePop && throwableModifier.ThrowerId == playerId)
                {
                    var throwable = entity.Require<ThrowableComponent>();
                    if (throwable.popTimeUs > throwable.thrownElapsedUs)
                        throwableModifier.PopQueued = true;
                }
            }
        }

        protected virtual void Release(SessionBase session, int playerId, ItemComponent item)
        {
            checked
            {
                Throw(session, playerId, itemName, m_ThrowablePrefab, m_ThrowForce);
                item.ammoInReserve.Value--;
            }
        }

        public static bool Throw(SessionBase session, int playerId, string itemName, IdBehavior throwablePrefab, float throwForce)
        {
            Container player = session.GetModifyingPayerFromId(playerId);
            if (player.Without<ServerTag>()) return false;

            Ray ray = SessionBase.GetRayForPlayer(player);
            var modifier = (EntityModifierBehavior) session.EntityManager.ObtainNextModifier(session.GetLatestSession(), throwablePrefab.id);
            if (modifier is ThrowableModifierBehavior throwableModifier)
            {
                throwableModifier.Name = itemName;
                modifier.transform.SetPositionAndRotation(ray.origin + ray.direction * 1.1f, Quaternion.LookRotation(ray.direction));
                throwableModifier.ThrowerId = playerId;
                Vector3 force = ray.direction * throwForce;
                if (player.With(out MoveComponent move)) force += move.velocity.Value * 0.1f;
                throwableModifier.Rigidbody.AddForce(force, ForceMode.Impulse);
            }
            return true;
        }

        protected internal override void OnUnequip(SessionBase session, int playerId, InventoryComponent inventory, ItemComponent item, uint durationUs)
        {
            if (item.status.id == ThrowableStatusId.Cooking)
                StartStatus(session, playerId, inventory, item, ItemStatusId.Idle, durationUs);
        }
    }
}