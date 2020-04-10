using System;
using Session.Player.Components;
using UnityEngine;

namespace Session.Items.Modifiers
{
    public static class ItemStatusId
    {
        public const byte Idle = 0,
                          PrimaryUsing = 1,
                          SecondaryUsing = 2,
                          Last = SecondaryUsing;
    }

    public static class ItemEquipStatusId
    {
        public const byte Unequipping = 0, Equipping = 1, Equipped = 2, Unequipped = 3;
    }

    public static class ItemId
    {
        public const byte None = 0,
                          TestingRifle = 1,
                          Last = TestingRifle;
    }

    [Serializable]
    public class ItemStatusModiferProperties
    {
        public float duration;
    }

    [CreateAssetMenu(fileName = "Item", menuName = "Item/Item", order = 0)]
    public class ItemModifierBase : ScriptableObject, IModifierBase<(ItemComponent, InventoryComponent), InputFlagProperty>
    {
        public byte id;
        public string itemName;
        public float movementFactor = 1.0f;
        [SerializeField] private ItemStatusModiferProperties[] m_StatusModiferProperties = default,
                                                               m_EquipStatusModiferProperties = default;

        public ItemStatusModiferProperties GetStatusModifierProperties(byte statusId) => m_StatusModiferProperties[statusId];

        public ItemStatusModiferProperties GetEquipStatusModifierProperties(byte equipStatusId) => m_EquipStatusModiferProperties[equipStatusId];

        public virtual void ModifyTrusted((ItemComponent, InventoryComponent) containerToModify, InputFlagProperty inputProperty, float duration)
        {
        }

        public virtual void ModifyChecked((ItemComponent, InventoryComponent) containerToModify, InputFlagProperty inputProperty, float duration)
        {
            (ItemComponent itemComponent, InventoryComponent inventoryComponent) = containerToModify;
            if (CanUse(itemComponent, inventoryComponent))
            {
                if (inputProperty.GetInput(PlayerInput.UseOne))
                    StartStatus(itemComponent, GetUseStatus(inputProperty));
                else if (HasSecondaryUse() && inputProperty.GetInput(PlayerInput.UseTwo))
                    StartStatus(itemComponent, ItemStatusId.SecondaryUsing);
            }
            ModifyStatus(containerToModify, inputProperty, duration);
        }

        private void ModifyStatus((ItemComponent, InventoryComponent) componentToModify, InputFlagProperty inputProperty, float duration)
        {
            (ItemComponent itemComponent, InventoryComponent inventoryComponent) = componentToModify;
            ByteStatusComponent status = itemComponent.status;
            status.elapsed.Value += duration;
            ItemStatusModiferProperties modifierProperties;
            while (status.elapsed > (modifierProperties = m_StatusModiferProperties[status.id]).duration)
            {
                float statusElapsed = status.elapsed;
                byte? nextStatus = FinishStatus(itemComponent, inventoryComponent, inputProperty);
                StartStatus(itemComponent, nextStatus ?? ItemStatusId.Idle, statusElapsed - modifierProperties.duration);
            }
        }

        protected void StartStatus(ItemComponent itemComponent, byte statusId, float elapsed = 0.0f)
        {
            itemComponent.status.id.Value = statusId;
            itemComponent.status.elapsed.Value = elapsed;
            switch (statusId)
            {
                case ItemStatusId.PrimaryUsing:
                    PrimaryUse(itemComponent);
                    break;
                case ItemStatusId.SecondaryUsing:
                    SecondaryUse();
                    break;
            }
        }

        /// <returns>State to switch to</returns>
        protected virtual byte? FinishStatus(ItemComponent itemComponent, InventoryComponent inventoryComponent, InputFlagProperty inputProperty)
        {
            switch (itemComponent.status.id)
            {
                case ItemStatusId.SecondaryUsing:
                    return ItemStatusId.Idle;
                case ItemStatusId.PrimaryUsing:
                    if (CanUse(itemComponent, inventoryComponent, true) && inputProperty.GetInput(PlayerInput.UseOne))
                        return GetUseStatus(inputProperty);
                    break;
            }
            return null;
        }

        protected virtual byte GetUseStatus(InputFlagProperty inputProperty) => ItemStatusId.PrimaryUsing;

        protected virtual bool CanUse(ItemComponent itemComponent, InventoryComponent inventoryComponent, bool justFinishedUse = false) =>
            (itemComponent.status.id != ItemStatusId.PrimaryUsing || justFinishedUse)
         && itemComponent.status.id != ItemStatusId.SecondaryUsing
         && inventoryComponent.equipStatus.id == ItemEquipStatusId.Equipped;

        protected virtual void SecondaryUse()
        {
        }

        protected virtual void PrimaryUse(ItemComponent itemComponent)
        {
        }

        public void ModifyCommands(InputFlagProperty commandsToModify)
        {
        }

        protected virtual bool HasSecondaryUse()
        {
            return false;
        }

        internal virtual void OnUnequip(ItemComponent itemComponent)
        {
        }
    }
}