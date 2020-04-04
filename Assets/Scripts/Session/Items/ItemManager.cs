using System.Linq;
using Collections;
using Session.Items.Modifiers;
using Session.Items.Visuals;
using Session.Player.Visualization;
using UnityEngine;
using UnityEngine.Playables;
using Util;

namespace Session.Items
{
    public class ItemManager : SingletonBehavior<ItemManager>
    {
        private ItemVisualBehavior[] m_ItemVisualPrefabs;
        private ItemModifierBase[] m_ItemModifiers;
        private Pool<ItemVisualBehavior>[] m_ItemVisualsPool;

        protected override void Awake()
        {
            base.Awake();
            m_ItemModifiers = Resources.LoadAll<ItemModifierBase>("Modifiers")
                                       .OrderBy(modifier => modifier.id).ToArray();
            m_ItemVisualPrefabs = Resources.LoadAll<GameObject>("Visuals")
                                           .Select(prefab => prefab.GetComponent<ItemVisualBehavior>())
                                           .Where(visuals => visuals != null)
                                           .OrderBy(visuals => visuals.Id).ToArray();
            m_ItemVisualsPool = Enumerable.Range(1, ItemId.Last)
                                          .Select(id => new Pool<ItemVisualBehavior>(0, () =>
                                           {
                                               ItemVisualBehavior visualsPrefab = m_ItemVisualPrefabs[id - 1],
                                                                  visualsInstance = Instantiate(visualsPrefab);
                                               visualsInstance.name = visualsPrefab.name;
                                               return visualsInstance.GetComponent<ItemVisualBehavior>();
                                           })).ToArray();
        }

        public ItemVisualBehavior ObtainVisuals(byte itemId, PlayerItemAnimatorBehavior playerItemAnimator, in PlayableGraph playerGraph)
        {
            Pool<ItemVisualBehavior> pool = m_ItemVisualsPool[itemId - 1];
            ItemVisualBehavior visual = pool.Obtain();
            visual.SetupForPlayerAnimation(playerItemAnimator, playerGraph);
            return visual;
        }

        public void ReturnVisuals(ItemVisualBehavior visual)
        {
            Pool<ItemVisualBehavior> pool = m_ItemVisualsPool[visual.Id - 1];
            visual.Cleanup();
            pool.Return(visual);
        }

        public ItemModifierBase GetModifier(byte itemId)
        {
            return m_ItemModifiers[itemId - 1];
        }
    }
}