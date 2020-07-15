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

        public virtual void ModifyChecked(SessionBase session, int playerId, Container player, ItemComponent item, InventoryComponent inventory,
                                          InputFlagProperty inputs, uint durationUs)
        {
            // TODO:refactor move frozen into CanUse
            bool notFrozen = player.Without(out FrozenProperty frozen) || frozen.WithoutValue || !frozen;
            if (notFrozen)
            {
                if (inputs.GetInput(PlayerInput.UseOne) && CanPrimaryUse(item, inventory))
                    StartStatus(session, playerId, inventory, item, GetUseStatus(inputs), durationUs);
                else if (inputs.GetInput(PlayerInput.UseTwo) && CanSecondaryUse(session, playerId, item, inventory))
                    StartStatus(session, playerId, inventory, item, ItemStatusId.SecondaryUsing, durationUs);
                else if (inputs.GetInput(PlayerInput.UseThree) && CanTernaryUse(session, playerId, item, inventory))
                    StartStatus(session, playerId, inventory, item, ItemStatusId.TernaryUsing, durationUs);
            }
            ModifyStatus(session, playerId, item, inventory, inputs, durationUs);
        }

        private void ModifyStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs, uint durationUs)
        {
            item.status.elapsedUs.Value += durationUs;
            StatusTick(session, playerId, inventory, item, inputs, durationUs);
            EndStatus(session, playerId, item, inventory, inputs, durationUs);
        }

        protected virtual void EndStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs, uint durationUs)
        {
            ByteStatusComponent status = item.status;
            try
            {
                ItemStatusModiferProperties modifierProperties;
                while (status.elapsedUs > (modifierProperties = m_StatusModiferProperties[status.id]).durationUs)
                {
                    if (modifierProperties.isPersistent) break;
                    uint statusElapsedUs = status.elapsedUs;
                    byte? nextStatus = FinishStatus(session, playerId, item, inventory, inputs);
                    StartStatus(session, playerId, inventory, item, nextStatus ?? ItemStatusId.Idle, durationUs, statusElapsedUs - modifierProperties.durationUs);
                    if (nextStatus == ItemStatusId.RequestRemoval) break;
                }
            }
            catch (Exception)
            {
                Debug.LogError($"Status ID: {status.id}");
                throw;
            }
        }

        protected virtual void StatusTick(SessionBase session, int playerId, InventoryComponent inventory, ItemComponent item, InputFlagProperty inputs, uint durationUs) { }

        protected void StartStatus(SessionBase session, int playerId, InventoryComponent inventory,
                                   ItemComponent item, byte statusId, uint durationUs, uint elapsedUs = 0u)
        {
            item.status.id.Value = statusId;
            item.status.elapsedUs.Value = elapsedUs;
            switch (statusId)
            {
                case ItemStatusId.PrimaryUsing:
                    PrimaryUse(session, playerId, inventory, item, durationUs);
                    break;
                case ItemStatusId.SecondaryUsing:
                    SecondaryUse(session, playerId, durationUs);
                    break;
                case ItemStatusId.TernaryUsing:
                    TernaryUse(session, playerId, item, durationUs);
                    break;
            }
        }

        /// <returns>State to switch to</returns>
        protected virtual byte? FinishStatus(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory, InputFlagProperty inputs)
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

        protected virtual bool CanSecondaryUse(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory) => false;

        protected virtual bool CanTernaryUse(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory) => false;

        protected virtual void SecondaryUse(SessionBase session, int playerId, uint durationUs) { }

        protected virtual void PrimaryUse(SessionBase session, int playerId, InventoryComponent inventory, ItemComponent item, uint durationUs) { }

        protected virtual void TernaryUse(SessionBase session, int playerId, ItemComponent item, uint durationUs) { }

        public void ModifyCommands(SessionBase session, InputFlagProperty commandsToModify) { }

        protected internal virtual void OnEquip(SessionBase session, int playerId, ItemComponent item, uint durationUs) { }

        protected internal virtual void OnUnequip(SessionBase session, int playerId, InventoryComponent inventory, ItemComponent item, uint durationUs) { }
    }
}