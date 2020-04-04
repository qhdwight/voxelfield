using System;
using System.Collections.Generic;
using Session.Player;
using Session.Player.Components;
using UnityEngine;
using Util;

namespace Session.Items.Modifiers
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
    
    [Serializable]
    public class ItemStatusModiferProperties
    {
        public float duration;
    }

    [CreateAssetMenu(fileName = "Item", menuName = "Item/Item", order = 0)]
    public class ItemModifier : ScriptableObject, IModifierBase<ItemComponent>
    {
        public ItemId id;
        public string itemName;
        public float movementFactor = 1.0f;
        [SerializeField] private ItemStatusModiferProperties[] m_StatusModiferProperties = default;

        private Dictionary<ItemStatusId, ItemStatusModiferProperties> m_StatusModifierProperties;

        public ItemStatusModiferProperties GetStatusModifierProperties(ItemStatusId statusId) => m_StatusModifierProperties[statusId];

        private void OnEnable()
        {
            m_StatusModifierProperties = m_StatusModiferProperties.ToEnumDictionary<ItemStatusId, ItemStatusModiferProperties>();
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
            ItemStatusModiferProperties modifierProperties;
            while (itemStatusComponent.elapsedTime > (modifierProperties = m_StatusModifierProperties[(ItemStatusId) itemStatusComponent.id.Value]).duration)
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

        protected virtual void PrimaryUse()
        {
            
        }

        protected virtual void StatusTick(PlayerCommandsComponent commands)
        {
        }

        public void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
        }
    }
}