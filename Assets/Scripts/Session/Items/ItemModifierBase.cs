using System;
using Session.Player;
using Session.Player.Components;
using UnityEngine;

namespace Session.Items
{
    public enum ItemStatusId
    {
        Unequipping,
        Equipping,
        Idle,
        PrimaryUsing,
        SecondaryUsing
    }

    public enum ItemId
    {
        None,
        TestingRifle,
    }

    public abstract class ItemModifierBase : IModifierBase<ItemComponent>
    {
        private readonly ItemModiferProperties m_ModiferProperties;

        internal ItemModifierBase(ItemModiferProperties modiferProperties)
        {
            m_ModiferProperties = modiferProperties;
        }

        public void ModifyTrusted(ItemComponent componentToModify, PlayerCommandsComponent commands)
        {
        }

        public void ModifyChecked(ItemComponent componentToModify, PlayerCommandsComponent commands)
        {
            ItemStatusComponent itemStatusComponent = componentToModify.status;
            float duration = commands.duration;
            itemStatusComponent.elapsedTime.Value += duration;
            StatusTick(commands);
            Func<byte, ItemStatusModiferProperties> obatiner = statusId => m_ModiferProperties.GetStatusModifierProperties((ItemStatusId) statusId);
            ItemStatusModiferProperties modifierProperties;
            while (itemStatusComponent.elapsedTime > (modifierProperties = obatiner(itemStatusComponent.id)).duration)
            {
                float statusElapsedTime = itemStatusComponent.elapsedTime;
                FinishStatus(itemStatusComponent, commands);
                itemStatusComponent.elapsedTime.Value = modifierProperties.duration < Mathf.Epsilon
                    ? statusElapsedTime
                    : statusElapsedTime % modifierProperties.duration;
            }
        }

        public void SetStatus(ItemStatusComponent itemStatusComponent, ItemStatusId statusId, float elapsedTime = 0.0f)
        {
            itemStatusComponent.id.Value = (byte) statusId;
            itemStatusComponent.elapsedTime.Value = elapsedTime;
        }

        public virtual void StartStatus(ItemStatusComponent itemStatusComponent, ItemStatusId statusId)
        {
            SetStatus(itemStatusComponent, statusId);
            switch (statusId)
            {
                case ItemStatusId.PrimaryUsing:
                    PrimaryUse();
                    break;
                case ItemStatusId.SecondaryUsing:
                    SecondaryUse();
                    break;
            }
        }

        protected virtual void FinishStatus(ItemStatusComponent itemStatusComponent, PlayerCommandsComponent commands)
        {
            var statusId = (ItemStatusId) itemStatusComponent.id.Value;
            switch (statusId)
            {
                case ItemStatusId.Equipping:
                case ItemStatusId.SecondaryUsing:
                    StartStatus(itemStatusComponent, ItemStatusId.Idle);
                    break;
                case ItemStatusId.Unequipping:
                    StartStatus(itemStatusComponent, ItemStatusId.PrimaryUsing);
                    break;
                case ItemStatusId.PrimaryUsing:
                    StartStatus(itemStatusComponent, CanUse(statusId, true) && commands.GetInput(PlayerInput.UseOne)
                                    ? GetUseStatus(commands)
                                    : ItemStatusId.Idle);
                    break;
            }
        }

        protected virtual ItemStatusId GetUseStatus(PlayerCommandsComponent commands) => ItemStatusId.PrimaryUsing;

        protected virtual bool CanUse(ItemStatusId statusId, bool justFinishedUse = false) =>
            (statusId != ItemStatusId.PrimaryUsing || justFinishedUse) &&
            statusId != ItemStatusId.SecondaryUsing &&
            statusId != ItemStatusId.Unequipping;

        protected virtual void SecondaryUse()
        {
        }

        protected abstract void PrimaryUse();

        protected virtual void StatusTick(PlayerCommandsComponent commands)
        {
        }

        public void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
        }
    }
}