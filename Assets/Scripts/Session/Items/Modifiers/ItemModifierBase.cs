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
        public const byte Unequipping = 0,
                          Equipping = 1,
                          Idle = 2,
                          PrimaryUsing = 3,
                          SecondaryUsing = 4,
                          Last = SecondaryUsing;
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
            componentToModify.statusElapsed.Value += duration;
            StatusTick(commands);
            ItemStatusModiferProperties modifierProperties;
            while (componentToModify.statusElapsed > (modifierProperties = m_StatusModiferProperties[componentToModify.statusId]).duration)
            {
                float statusElapsed = componentToModify.statusElapsed;
                FinishStatus(componentToModify, commands);
                componentToModify.statusElapsed.Value = Mathf.Approximately(modifierProperties.duration, 0.0f)
                    ? statusElapsed
                    : statusElapsed % modifierProperties.duration;
            }
        }

        public void SetStatus(ItemComponent itemComponent, byte statusId, float elapsed = 0.0f)
        {
            itemComponent.statusId.Value = statusId;
            itemComponent.statusElapsed.Value = elapsed;
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
            byte statusId = itemComponent.statusId;
            switch (statusId)
            {
                case ItemStatusId.Equipping:
                case ItemStatusId.SecondaryUsing:
                    StartStatus(itemComponent, ItemStatusId.Idle);
                    break;
                case ItemStatusId.Unequipping:
                    StartStatus(itemComponent, ItemStatusId.PrimaryUsing);
                    break;
                case ItemStatusId.PrimaryUsing:
                    StartStatus(itemComponent, CanUse(itemComponent, true) && commands.GetInput(PlayerInput.UseOne) ? GetUseStatus(commands) : ItemStatusId.Idle);
                    break;
            }
        }

        protected virtual byte GetUseStatus(PlayerCommandsComponent commands) => ItemStatusId.PrimaryUsing;

        protected virtual bool CanUse(ItemComponent itemComponent, bool justFinishedUse = false) =>
            (itemComponent.statusId != ItemStatusId.PrimaryUsing || justFinishedUse) &&
            itemComponent.statusId != ItemStatusId.SecondaryUsing &&
            itemComponent.statusId != ItemStatusId.Unequipping;

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