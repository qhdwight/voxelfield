using System;
using System.Collections.Generic;
using System.Linq;
using Collections;
using Session.Player.Visualization;
using UnityEngine;
using UnityEngine.Playables;
using Util;

namespace Session.Items
{
    public class ItemManager : SingletonBehavior<ItemManager>
    {
        private Dictionary<ItemId, ItemVisualBehavior> m_ItemVisualPrefabs;
        private Dictionary<ItemId, ItemModiferProperties> m_ModifierStatusProperties;
        private Dictionary<ItemId, Pool<ItemVisualBehavior>> m_ItemVisualsPool;

        protected override void Awake()
        {
            base.Awake();
            m_ModifierStatusProperties = Resources.LoadAll<ItemModiferProperties>($"Modifiers")
                                                  .ToDictionary(properties => properties.id, properties => properties);
            m_ItemVisualPrefabs = Resources.LoadAll<GameObject>($"Visuals")
                                           .Select(prefab => prefab.GetComponent<ItemVisualBehavior>())
                                           .Where(visuals => visuals != null)
                                           .ToDictionary(visuals => visuals.Id, visuals => visuals);
            var items = (ItemId[]) Enum.GetValues(typeof(ItemId));
            m_ItemVisualsPool = items.Where(item => item != ItemId.None)
                                     .ToDictionary(item => item,
                                                   item => new Pool<ItemVisualBehavior>(0, () =>
                                                   {
                                                       ItemVisualBehavior visualsPrefab = m_ItemVisualPrefabs[item],
                                                                          visualsInstance = Instantiate(visualsPrefab);
                                                       visualsInstance.name = visualsPrefab.name;
                                                       return visualsInstance.GetComponent<ItemVisualBehavior>();
                                                   }));
        }

        public ItemVisualBehavior ObtainVisuals(ItemId itemId, PlayerItemAnimatorBehavior playerItemAnimator, PlayableGraph playerGraph)
        {
            Pool<ItemVisualBehavior> pool = m_ItemVisualsPool[itemId];
            ItemVisualBehavior visual = pool.Obtain();
            visual.Setup(playerItemAnimator, playerGraph);
            return visual;
        }

        public void ReturnVisuals(ItemVisualBehavior visual)
        {
            Pool<ItemVisualBehavior> pool = m_ItemVisualsPool[visual.Id];
            visual.Cleanup();
            pool.Return(visual);
        }

        public ItemModiferProperties GetModifierProperties(ItemId itemId)
        {
            return m_ModifierStatusProperties[itemId];
        }
    }
}