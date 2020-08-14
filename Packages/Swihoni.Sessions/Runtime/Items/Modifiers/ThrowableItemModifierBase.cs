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

    public class ThrowableItemModifierBase : ItemModifierBase
    {
        [Header("Throwable"), SerializeField] private float m_ThrowForce = default;
        [SerializeField] protected ThrowableModifierBehavior m_ThrowablePrefab = default;

        protected override void StatusTick(in SessionContext context, InventoryComponent inventory, ItemComponent item, InputFlagProperty inputs)
        {
            if (item.status.id == ThrowableStatusId.Cooking && !inputs.GetInput(PlayerInput.UseOne))
                StartStatus(context, inventory, item, ItemStatusId.PrimaryUsing);
        }

        protected override byte? FinishStatus(in SessionContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            // TODO:refactor make base class for C4 type objects
            if (item.status.id == ItemStatusId.PrimaryUsing)
            {
                Release(context, item);
                if (item.ammoInReserve == 0)
                {
                    if (item.id != ItemId.C4) return ItemStatusId.RequestRemoval;
                }
                else
                {
                    inventory.equipStatus.id.Value = ItemEquipStatusId.Equipping;
                    inventory.equipStatus.elapsedUs.Value = 0u;
                }
            }
            else if (item.status.id == ItemStatusId.SecondaryUsing && item.id == ItemId.C4 && item.ammoInReserve == 0)
                return ItemStatusId.RequestRemoval;
            return base.FinishStatus(context, item, inventory, inputs);
        }

        protected override byte GetUseStatus(InputFlagProperty input) => ThrowableStatusId.Cooking;

        protected override bool CanPrimaryUse(ItemComponent item, InventoryComponent inventory, bool justFinishedUse = false)
            => base.CanPrimaryUse(item, inventory, justFinishedUse) && item.status.id != ThrowableStatusId.Cooking && item.ammoInReserve > 0;

        protected override bool CanSecondaryUse(in SessionContext context, ItemComponent item, InventoryComponent inventory)
            => base.CanPrimaryUse(item, inventory) && item.status.id != ThrowableStatusId.Cooking;

        protected override void PrimaryUse(in SessionContext context, InventoryComponent inventory, ItemComponent item) { }

        protected override void SecondaryUse(in SessionContext context)
        {
            var entities = context.sessionContainer.Require<EntityArray>();
            for (var index = 0; index < entities.Length; index++)
            {
                EntityContainer entity = entities[index];
                if (!(context.session.EntityManager.UnsafeModifiers[index] is ThrowableModifierBehavior throwableModifier)) continue;

                var throwable = entity.Require<ThrowableComponent>();
                if (throwableModifier.CanQueuePop && throwable.throwerId == context.playerId)
                {
                    if (throwable.popTimeUs > throwable.thrownElapsedUs)
                        throwableModifier.PopQueued = true;
                }
            }
        }

        protected virtual void Release(in SessionContext context, ItemComponent item)
        {
            checked
            {
                Throw(context, itemName, m_ThrowablePrefab, m_ThrowForce);
                item.ammoInReserve.Value--;
            }
        }

        public static bool Throw(in SessionContext context, string itemName, IdBehavior throwablePrefab, float throwForce)
        {
            Container player = context.player;
            if (player.Without<ServerTag>()) return false;

            Ray ray = player.GetRayForPlayer();
            (ModifierBehaviorBase modifier, Container entity) = context.session.EntityManager.ObtainNextModifier(context.sessionContainer, throwablePrefab.id);
            if (modifier is ThrowableModifierBehavior throwableModifier && entity.With(out ThrowableComponent throwable))
            {
                throwableModifier.Name = itemName;
                throwable.throwerId.Value = (byte) context.playerId;

                Vector3 position = ray.origin + ray.direction * 1.2f;
                Quaternion rotation = Quaternion.LookRotation(ray.direction);
                modifier.transform.SetPositionAndRotation(position, rotation);
                throwable.position.Value = position;
                throwable.rotation.Value = rotation;

                Vector3 force = ray.direction * throwForce;
                if (player.With(out MoveComponent move)) force += move.velocity.Value * 0.1f;
                throwableModifier.Rigidbody.AddForce(force, ForceMode.Impulse);
            }
            return true;
        }

        protected internal override void OnUnequip(in SessionContext context, InventoryComponent inventory, ItemComponent item)
        {
            if (item.status.id == ThrowableStatusId.Cooking)
                StartStatus(context, inventory, item, ItemStatusId.Idle);
        }
    }
}