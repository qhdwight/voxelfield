using System;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Items.Modifiers
{
    public static class ItemStatusId
    {
        public const byte Idle = 0,
                          PrimaryUsing = 1,
                          SecondaryUsing = 2,
                          TernaryUsing = 3,
                          Last = TernaryUsing,
                          RequestRemoval = byte.MaxValue;
    }

    public static class ItemEquipStatusId
    {
        public const byte Unequipping = 0,
                          Equipping = 1,
                          Equipped = 2,
                          Unequipped = 3;
    }

    public static class ItemId
    {
        public const byte None = 0,
                          Rifle = 1,
                          Grenade = 2,
                          Molotov = 3,
                          Shotgun = 4,
                          C4 = 5,
                          Pickaxe = 6,
                          Pistol = 7,
                          Sniper = 8,
                          VoxelWand = 9,
                          ModelWand = 10,
                          Deagle = 11,
                          GrenadeLauncher = 12,
                          MissileLauncher = 13,
                          Smg = 14,
                          SuperPickaxe = 15,
                          ImpactGrenade = 16;
    }

    [Serializable]
    public class ItemStatusModiferProperties
    {
        public uint durationUs;
        public bool isPersistent;
    }

    [CreateAssetMenu(fileName = "Item", menuName = "Item/Item", order = 0)]
    public class ItemModifierBase : ScriptableObject
    {
        public byte id;
        public string itemName;
        public float movementFactor = 1.0f;
        [SerializeField] protected ItemStatusModiferProperties[] m_StatusModiferProperties, m_EquipStatusModiferProperties;
        public ItemStatusModiferProperties GetStatusModifierProperties(byte statusId) => m_StatusModiferProperties[statusId];

        public ItemStatusModiferProperties GetEquipStatusModifierProperties(byte equipStatusId) => m_EquipStatusModiferProperties[equipStatusId];

        public virtual void ModifyChecked(in ModifyContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            // TODO:refactor move frozen into CanUse
            int playerId = context.playerId;
            SessionBase session = context.session;
            bool notFrozen = context.player.Without(out FrozenProperty frozen) || frozen.WithoutValue || !frozen;
            if (notFrozen)
            {
                if (inputs.GetInput(PlayerInput.UseOne) && CanPrimaryUse(item, inventory))
                    StartStatus(context, inventory, item, GetUseStatus(inputs));
                else if (inputs.GetInput(PlayerInput.UseTwo) && CanSecondaryUse(context, item, inventory))
                    StartStatus(context, inventory, item, ItemStatusId.SecondaryUsing);
                else if (inputs.GetInput(PlayerInput.UseThree) && CanTernaryUse(context, item, inventory))
                    StartStatus(context, inventory, item, ItemStatusId.TernaryUsing);
            }
            ModifyStatus(context, item, inventory, inputs);
        }

        private void ModifyStatus(in ModifyContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            item.status.elapsedUs.Value += context.durationUs;
            StatusTick(context, inventory, item, inputs);
            EndStatus(context, item, inventory, inputs);
        }

        protected virtual void EndStatus(in ModifyContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            ByteStatusComponent status = item.status;
            try
            {
                ItemStatusModiferProperties modifierProperties;
                while (status.elapsedUs > (modifierProperties = m_StatusModiferProperties[status.id]).durationUs)
                {
                    if (modifierProperties.isPersistent) break;
                    uint statusElapsedUs = status.elapsedUs;
                    byte? nextStatus = FinishStatus(context, item, inventory, inputs);
                    StartStatus(context, inventory, item, nextStatus ?? ItemStatusId.Idle, statusElapsedUs - modifierProperties.durationUs);
                    if (nextStatus == ItemStatusId.RequestRemoval) break;
                }
            }
            catch (Exception)
            {
                Debug.LogError($"Status ID: {status.id}");
                throw;
            }
        }

        protected virtual void StatusTick(in ModifyContext context, InventoryComponent inventory, ItemComponent item, InputFlagProperty inputs) { }

        protected void StartStatus(in ModifyContext context, InventoryComponent inventory, ItemComponent item, byte statusId, uint elapsedUs = 0u)
        {
            item.status.id.Value = statusId;
            item.status.elapsedUs.Value = elapsedUs;
            switch (statusId)
            {
                case ItemStatusId.PrimaryUsing:
                    PrimaryUse(context, inventory, item);
                    break;
                case ItemStatusId.SecondaryUsing:
                    SecondaryUse(context);
                    break;
                case ItemStatusId.TernaryUsing:
                    TernaryUse(context, item);
                    break;
            }
        }

        /// <returns>State to switch to</returns>
        protected virtual byte? FinishStatus(in ModifyContext context, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
        {
            switch (item.status.id)
            {
                case ItemStatusId.SecondaryUsing:
                    return ItemStatusId.Idle;
                case ItemStatusId.PrimaryUsing:
                    if (CanPrimaryUse(item, inventory, true) && inputs.GetInput(PlayerInput.UseOne))
                        return GetUseStatus(inputs);
                    break;
            }
            return null;
        }

        protected virtual byte GetUseStatus(InputFlagProperty input) => ItemStatusId.PrimaryUsing;

        protected virtual bool CanPrimaryUse(ItemComponent item, InventoryComponent inventory, bool justFinishedUse = false) =>
            (item.status.id != ItemStatusId.PrimaryUsing || justFinishedUse)
         && item.status.id != ItemStatusId.SecondaryUsing
         && item.status.id != ItemStatusId.TernaryUsing
         && inventory.equipStatus.id == ItemEquipStatusId.Equipped;

        protected virtual bool CanSecondaryUse(in ModifyContext context, ItemComponent item, InventoryComponent inventory) => false;

        protected virtual bool CanTernaryUse(in ModifyContext context, ItemComponent item, InventoryComponent inventory) => false;

        protected virtual void SecondaryUse(in ModifyContext context) { }

        protected virtual void PrimaryUse(in ModifyContext context, InventoryComponent inventory, ItemComponent item) { }

        protected virtual void TernaryUse(in ModifyContext context, ItemComponent item) { }

        public void ModifyCommands(SessionBase session, InputFlagProperty commandsToModify) { }

        protected internal virtual void OnEquip(in ModifyContext context, ItemComponent item) { }

        protected internal virtual void OnUnequip(in ModifyContext context, InventoryComponent inventory, ItemComponent item) { }
    }
}