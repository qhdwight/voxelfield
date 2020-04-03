using System;
using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Session.Items
{
    [Serializable]
    public class ItemStatusModiferProperties
    {
        public float duration;
    }

    [CreateAssetMenu(fileName = "Item", menuName = "Item/Item", order = 0)]
    public class ItemModiferProperties : ScriptableObject
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
    }
}