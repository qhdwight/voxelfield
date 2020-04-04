using System;
using System.Collections.Generic;
using Session.Player;
using Session.Player.Components;
using UnityEngine;
using Util;

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
        public const byte Unequipping = 0, Equipping = 1, Ready = 2;
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
        [SerializeField] private ItemStatusModiferProperties[] m_StatusModiferProperties = default;

        public ItemStatusModiferProperties GetStatusModifierProperties(byte statusId) => m_StatusModiferProperties[statusId];

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
            float duration = commands.duration;
            componentToModify.status.elapsed.Value += duration;
            StatusTick(commands);
            ItemStatusModiferProperties modifierProperties;
            while (componentToModify.status.elapsed > (modifierProperties = m_StatusModiferProperties[componentToModify.status.id]).duration)
            {
                float statusElapsed = componentToModify.status.elapsed;
                FinishStatus(componentToModify, commands);
                componentToModify.status.elapsed.Value = Mathf.Approximately(modifierProperties.duration, 0.0f)
                    ? statusElapsed
                    : statusElapsed % modifierProperties.duration;
            }
        }

        public void SetStatus(ItemComponent itemComponent, byte statusId, float elapsed = 0.0f)
        {
            itemComponent.status.id.Value = statusId;
            itemComponent.status.elapsed.Value = elapsed;
        }

        public virtual void StartStatus(ItemComponent itemComponent, byte statusId)
        {
            SetStatus(itemComponent, statusId);
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

        protected virtual void FinishStatus(ItemComponent itemComponent, PlayerCommandsComponent commands)
        {
            byte statusId = itemComponent.status.id;
            switch (statusId)
            {
                case ItemStatusId.SecondaryUsing:
                    StartStatus(itemComponent, ItemStatusId.Idle);
                    break;
                case ItemStatusId.PrimaryUsing:
                    StartStatus(itemComponent, CanUse(itemComponent, true) && commands.GetInput(PlayerInput.UseOne) ? GetUseStatus(commands) : ItemStatusId.Idle);
                    break;
            }
        }

        protected virtual byte GetUseStatus(PlayerCommandsComponent commands) => ItemStatusId.PrimaryUsing;

        protected virtual bool CanUse(ItemComponent itemComponent, bool justFinishedUse = false) =>
            (itemComponent.status.id != ItemStatusId.PrimaryUsing || justFinishedUse) &&
            itemComponent.status.id != ItemStatusId.SecondaryUsing &&
            itemComponent.equipStatus.id != ItemEquipStatusId.Unequipping;

        protected virtual void SecondaryUse()
        {
        }

        protected virtual void PrimaryUse(ItemComponent itemComponent)
        {
        }

        protected virtual void StatusTick(PlayerCommandsComponent commands)
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