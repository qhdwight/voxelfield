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
            float duration = commands.duration;
            componentToModify.statusElapsed.Value += duration;
            StatusTick(commands);
            ItemStatusModiferProperties modifierProperties;
            while (componentToModify.statusElapsed > (modifierProperties = m_StatusModifierProperties[(ItemStatusId) componentToModify.statusId.Value]).duration)
            {
                float statusElapsed = componentToModify.statusElapsed;
                FinishStatus(componentToModify, commands);
                componentToModify.statusElapsed.Value = Mathf.Approximately(modifierProperties.duration, 0.0f)
                    ? statusElapsed
                    : statusElapsed % modifierProperties.duration;
            }
        }

        public void SetStatus(ItemComponent itemComponent, ItemStatusId statusId, float elapsed = 0.0f)
        {
            itemComponent.statusId.Value = (byte) statusId;
            itemComponent.statusElapsed.Value = elapsed;
        }

        public virtual void StartStatus(ItemComponent itemComponent, ItemStatusId statusId)
        {
            SetStatus(itemComponent, statusId);
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

        protected virtual void FinishStatus(ItemComponent itemComponent, PlayerCommandsComponent commands)
        {
            var statusId = (ItemStatusId) itemComponent.id.Value;
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
                    StartStatus(itemComponent, CanUse(statusId, true) && commands.GetInput(PlayerInput.UseOne)
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