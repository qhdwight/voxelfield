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
        private static ItemModifierBase[] _itemModifiers;
        private static Pool<ItemVisualBehavior>[] _itemVisualPool;

        [RuntimeInitializeOnLoadMethod]
        private static void Setup()
        {
            _itemModifiers = Resources.LoadAll<ItemModifierBase>("Modifiers")
                                      .OrderBy(modifier => modifier.id).ToArray();
            ItemVisualBehavior[] itemVisualPrefabs = Resources.LoadAll<GameObject>("Visuals")
                                                              .Select(prefab => prefab.GetComponent<ItemVisualBehavior>())
                                                              .Where(visuals => visuals != null)
                                                              .OrderBy(visuals => visuals.Id).ToArray();
            _itemVisualPool = Enumerable.Range(1, ItemId.Last)
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
            Pool<ItemVisualBehavior> pool = _itemVisualPool[itemId - 1];
            ItemVisualBehavior visual = pool.Obtain();
            visual.SetupForPlayerAnimation(playerItemAnimator, playerGraph);
            return visual;
        }

        public static void ReturnVisuals(ItemVisualBehavior visual)
        {
            Pool<ItemVisualBehavior> pool = _itemVisualPool[visual.Id - 1];
            visual.Cleanup();
            visual.SetRenderingMode(false);
            pool.Return(visual);
        }

        public static ItemModifierBase GetModifier(byte itemId) { return _itemModifiers[itemId - 1]; }
    }
}