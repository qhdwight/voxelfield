using System.Linq;
using Swihoni.Collections;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Items.Visuals;
using Swihoni.Sessions.Player.Visualization;
using Swihoni.Util;
using UnityEngine;
using UnityEngine.Playables;

namespace Swihoni.Sessions.Items
{
    public static class ItemResourceManager
    {
        internal static readonly ItemVisualBehavior[] ItemVisualPrefabs;
        internal static readonly ItemModifierBase[] ItemModifiers;

        static ItemResourceManager()
        {
            ItemModifiers = Resources.LoadAll<ItemModifierBase>("Modifiers")
                                     .OrderBy(modifier => modifier.id).ToArray();
            ItemVisualPrefabs = Resources.LoadAll<GameObject>("Visuals")
                                         .Select(prefab => prefab.GetComponent<ItemVisualBehavior>())
                                         .Where(visuals => visuals != null)
                                         .OrderBy(visuals => visuals.Id).ToArray();
        }
    }

    public class ItemManager : SingletonBehavior<ItemManager>
    {
        private Pool<ItemVisualBehavior>[] m_ItemVisualsPool;

        protected override void Awake()
        {
            base.Awake();
            m_ItemVisualsPool = Enumerable.Range(1, ItemId.Last)
                                          .Select(id => new Pool<ItemVisualBehavior>(0, () =>
                                           {
                                               ItemVisualBehavior visualsPrefab = ItemResourceManager.ItemVisualPrefabs[id - 1],
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
            visual.SetRenderingMode(false);
            pool.Return(visual);
        }

        public static ItemModifierBase GetModifier(byte itemId) { return ItemResourceManager.ItemModifiers[itemId - 1]; }
    }
}