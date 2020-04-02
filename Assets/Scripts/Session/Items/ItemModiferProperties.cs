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

        private Dictionary<ItemStatus, ItemStatusModiferProperties> m_StatusModifierProperties;

        public ItemStatusModiferProperties GetStatusModifierProperties(ItemStatus status) => m_StatusModifierProperties[status];

        private void OnEnable()
        {
            m_StatusModifierProperties = m_StatusModiferProperties.ToEnumDictionary<ItemStatus, ItemStatusModiferProperties>();
        }
    }
}