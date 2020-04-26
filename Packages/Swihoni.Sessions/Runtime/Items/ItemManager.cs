using System.Linq;
using Swihoni.Collections;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Items.Visuals;
using Swihoni.Sessions.Player.Visualization;
using UnityEngine;
using UnityEngine.Playables;

namespace Swihoni.Sessions.Items
{
    public static class ItemManager
    {
        private static readonly ItemModifierBase[] ItemModifiers;
        private static readonly Pool<ItemVisualBehavior>[] ItemVisualPool;

        static ItemManager()
        {
            ItemModifiers = Resources.LoadAll<ItemModifierBase>("Modifiers")
                                     .OrderBy(modifier => modifier.id).ToArray();
            ItemVisualBehavior[] itemVisualPrefabs = Resources.LoadAll<GameObject>("Visuals")
                                                              .Select(prefab => prefab.GetComponent<ItemVisualBehavior>())
                                                              .Where(visuals => visuals != null)
                                                              .OrderBy(visuals => visuals.Id).ToArray();
            ItemVisualPool = Enumerable.Range(1, ItemId.Last)
                                       .Select(id => new Pool<ItemVisualBehavior>(0, () =>
                                        {
                                            ItemVisualBehavior visualsPrefab = itemVisualPrefabs[id - 1],
                                                               visualsInstance = Object.Instantiate(visualsPrefab);
                                            visualsInstance.name = visualsPrefab.name;
                                            return visualsInstance.GetComponent<ItemVisualBehavior>();
                                        })).ToArray();
        }

        public static ItemVisualBehavior ObtainVisuals(byte itemId, PlayerItemAnimatorBehavior playerItemAnimator, in PlayableGraph playerGraph)
        {
            Pool<ItemVisualBehavior> pool = ItemVisualPool[itemId - 1];
            ItemVisualBehavior visual = pool.Obtain();
            visual.SetupForPlayerAnimation(playerItemAnimator, playerGraph);
            return visual;
        }

        public static void ReturnVisuals(ItemVisualBehavior visual)
        {
            Pool<ItemVisualBehavior> pool = ItemVisualPool[visual.Id - 1];
            visual.Cleanup();
            visual.SetRenderingMode(false);
            pool.Return(visual);
        }

        public static ItemModifierBase GetModifier(byte itemId) { return ItemModifiers[itemId - 1]; }
    }
}