using System;
using Session.Player;
using Session.Player.Components;
using UnityEngine;

namespace Session.Items.Modifiers
{
    public static class ItemStatusId
    {
        public const byte Idle = 0,
                          PrimaryUsing = 1, SecondaryUsing = 2,
                          Last = SecondaryUsing;
    }

    public static class ItemEquipStatusId
    {
        public const byte Unequipping = 0, Equipping = 1, Inactive = 2, Ready = 3;
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
    public class ItemModifierBase : ScriptableObject, IModifierBase<ItemComponent>
    {
        public byte id;
        public string itemName;
        public float movementFactor = 1.0f;
        [SerializeField] private ItemStatusModiferProperties[] m_StatusModiferProperties = default,
                                                               m_EquipStatusModiferProperties = default;

        public ItemStatusModiferProperties GetStatusModifierProperties(byte statusId) => m_StatusModiferProperties[statusId];

        public ItemStatusModiferProperties GetEquipStatusModifierProperties(byte equipStatusId) => m_EquipStatusModiferProperties[equipStatusId];

        public virtual void ModifyTrusted(ItemComponent componentToModify, PlayerCommandsComponent commands)
        {
        }

        public virtual void ModifyChecked(ItemComponent componentToModify, PlayerCommandsComponent commands)
        {
            if (CanUse(componentToModify))
            {
                if (commands.GetInput(PlayerInput.UseOne))
                    StartStatus(componentToModify, GetUseStatus(commands));
                else if (HasSecondaryUse() && commands.GetInput(PlayerInput.UseOne))
                    StartStatus(componentToModify, ItemStatusId.SecondaryUsing);
            }
            ModifyEquipStatus(componentToModify, commands);
            ModifyStatus(componentToModify, commands);
        }

        private void ModifyStatus(ItemComponent componentToModify, PlayerCommandsComponent commands)
        {
            ItemByteStatusComponent status = componentToModify.status;
            status.elapsed.Value += commands.duration;
            ItemStatusModiferProperties modifierProperties;
            while (status.elapsed > (modifierProperties = m_StatusModiferProperties[status.id]).duration)
            {
                float statusElapsed = status.elapsed;
                byte? nextStatus = FinishStatus(componentToModify, commands);
                StartStatus(componentToModify, nextStatus ?? ItemStatusId.Idle, statusElapsed - modifierProperties.duration);
                if (Mathf.Approximately(modifierProperties.duration, 0.0f))
                    break;
            }
        }

        private void ModifyEquipStatus(ItemComponent componentToModify, PlayerCommandsComponent commands)
        {
            ItemByteStatusComponent equipStatus = componentToModify.equipStatus;
            equipStatus.elapsed.Value += commands.duration;
            ItemStatusModiferProperties modifierProperties;
            while (equipStatus.elapsed > (modifierProperties = m_EquipStatusModiferProperties[equipStatus.id]).duration)
            {
                if (equipStatus.id == ItemEquipStatusId.Equipping) equipStatus.id.Value = ItemEquipStatusId.Ready;
                else if (equipStatus.id == ItemEquipStatusId.Unequipping) equipStatus.id.Value = ItemEquipStatusId.Inactive;
                if (Mathf.Approximately(modifierProperties.duration, 0.0f))
                    break;
                equipStatus.elapsed.Value -= modifierProperties.duration;
            }
        }

        private static void SetStatus(ItemComponent itemComponent, byte statusId, float elapsed = 0.0f)
        {
            itemComponent.status.id.Value = statusId;
            itemComponent.status.elapsed.Value = elapsed;
        }

        protected void StartStatus(ItemComponent itemComponent, byte statusId, float elapsed = 0.0f)
        {
            SetStatus(itemComponent, statusId, elapsed);
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

        protected virtual byte? FinishStatus(ItemComponent itemComponent, PlayerCommandsComponent commands)
        {
            switch (itemComponent.status.id)
            {
                case ItemStatusId.SecondaryUsing:
                    return ItemStatusId.Idle;
                case ItemStatusId.PrimaryUsing:
                    if (CanUse(itemComponent, true) && commands.GetInput(PlayerInput.UseOne))
                        return GetUseStatus(commands);
                    break;
            }
            return null;
        }

        protected virtual byte GetUseStatus(PlayerCommandsComponent commands) => ItemStatusId.PrimaryUsing;

        protected virtual bool CanUse(ItemComponent itemComponent, bool justFinishedUse = false) =>
            (itemComponent.status.id != ItemStatusId.PrimaryUsing || justFinishedUse) &&
            itemComponent.status.id != ItemStatusId.SecondaryUsing &&
            itemComponent.equipStatus.id == ItemEquipStatusId.Ready;

        protected virtual void SecondaryUse()
        {
        }

        protected virtual void PrimaryUse(ItemComponent itemComponent)
        {
        }

        public void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
        }

        protected virtual bool HasSecondaryUse()
        {
            return false;
        }
    }
}