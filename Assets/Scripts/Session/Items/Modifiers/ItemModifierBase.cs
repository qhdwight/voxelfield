using System;
using Session.Player;
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
    public class ItemModifierBase : ScriptableObject, IModifierBase<(ItemComponent, ByteStatusComponent)>
    {
        public byte id;
        public string itemName;
        public float movementFactor = 1.0f;
        [SerializeField] private ItemStatusModiferProperties[] m_StatusModiferProperties = default,
                                                               m_EquipStatusModiferProperties = default;

        public ItemStatusModiferProperties GetStatusModifierProperties(byte statusId) => m_StatusModiferProperties[statusId];

        public ItemStatusModiferProperties GetEquipStatusModifierProperties(byte equipStatusId) => m_EquipStatusModiferProperties[equipStatusId];

        public virtual void ModifyTrusted((ItemComponent, ByteStatusComponent) componentToModify, PlayerCommandsComponent commands)
        {
        }

        public virtual void ModifyChecked((ItemComponent, ByteStatusComponent) componentToModify, PlayerCommandsComponent commands)
        {
            (ItemComponent itemComponent, ByteStatusComponent equipStatus) = componentToModify;
            if (CanUse(itemComponent, equipStatus))
            {
                if (commands.GetInput(PlayerInput.UseOne))
                    StartStatus(itemComponent, GetUseStatus(commands));
                else if (HasSecondaryUse() && commands.GetInput(PlayerInput.UseTwo))
                    StartStatus(itemComponent, ItemStatusId.SecondaryUsing);
            }
            ModifyStatus(componentToModify, commands);
        }

        private void ModifyStatus((ItemComponent, ByteStatusComponent) componentToModify, PlayerCommandsComponent commands)
        {
            (ItemComponent itemComponent, ByteStatusComponent equipStatus) = componentToModify;
            ByteStatusComponent status = itemComponent.status;
            status.elapsed.Value += commands.duration;
            ItemStatusModiferProperties modifierProperties;
            while (status.elapsed > (modifierProperties = m_StatusModiferProperties[status.id]).duration)
            {
                float statusElapsed = status.elapsed;
                byte? nextStatus = FinishStatus(itemComponent, equipStatus, commands);
                StartStatus(itemComponent, nextStatus ?? ItemStatusId.Idle, elapsed: statusElapsed - modifierProperties.duration);
                if (Mathf.Approximately(modifierProperties.duration, 0.0f))
                    break;
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

        protected virtual byte? FinishStatus(ItemComponent itemComponent, ByteStatusComponent equipStatus, PlayerCommandsComponent commands)
        {
            switch (itemComponent.status.id)
            {
                case ItemStatusId.SecondaryUsing:
                    return ItemStatusId.Idle;
                case ItemStatusId.PrimaryUsing:
                    if (CanUse(itemComponent, equipStatus, true) && commands.GetInput(PlayerInput.UseOne))
                        return GetUseStatus(commands);
                    break;
            }
            return null;
        }

        protected virtual byte GetUseStatus(PlayerCommandsComponent commands) => ItemStatusId.PrimaryUsing;

        protected virtual bool CanUse(ItemComponent itemComponent, ByteStatusComponent equipStatus, bool justFinishedUse = false) =>
            (itemComponent.status.id != ItemStatusId.PrimaryUsing || justFinishedUse) && itemComponent.status.id != ItemStatusId.SecondaryUsing
                                                                                      && equipStatus.id == ItemEquipStatusId.Equipped;

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

        internal virtual void OnUnequip(ItemComponent itemComponent)
        {
        }
    }
}