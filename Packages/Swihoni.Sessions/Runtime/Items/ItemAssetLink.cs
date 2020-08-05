using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Collections;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Items.Visuals;
using Swihoni.Sessions.Player.Visualization;
using UnityEngine;
using UnityEngine.Playables;
using Object = UnityEngine.Object;

namespace Swihoni.Sessions.Items
{
    public static class ItemAssetLink
    {
        private static ItemModifierBase[] _itemModifiers;
        private static Pool<ItemVisualBehavior>[] _itemVisualPools;
        private static ItemVisualBehavior[] _itemVisualPrefabs;

        public static IReadOnlyList<ItemModifierBase> ItemVisualPrefabs => _itemModifiers;

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            _itemModifiers = Resources.LoadAll<ItemModifierBase>("Items")
                                      .OrderBy(modifier => modifier.id).ToArray();
            _itemVisualPrefabs = Resources.LoadAll<GameObject>("Items")
                                          .Select(prefab => prefab.GetComponent<ItemVisualBehavior>())
                                          .Where(visuals => visuals != null && visuals.Id > 0)
                                          .OrderBy(visuals => visuals.Id).ToArray();
            _itemVisualPools = _itemVisualPrefabs
                              .Select(visualPrefab => new Pool<ItemVisualBehavior>(0, () =>
                               {
                                   ItemVisualBehavior visualsInstance = Object.Instantiate(visualPrefab);
                                   visualsInstance.name = "Visual";
                                   return visualsInstance;
                               })).ToArray();
        }

        public static ItemVisualBehavior ObtainVisuals(byte itemId, PlayerItemAnimatorBehavior playerItemAnimator, in PlayableGraph playerGraph)
        {
            Pool<ItemVisualBehavior> pool = _itemVisualPools[itemId - 1];
            ItemVisualBehavior visual = pool.Obtain();
            visual.SetupForPlayerAnimation(playerItemAnimator, playerGraph);
            return visual;
        }

        public static void ReturnVisuals(ItemVisualBehavior visual)
        {
            Pool<ItemVisualBehavior> pool = _itemVisualPools[visual.Id - 1];
            visual.Cleanup();
            visual.SetActive(false);
            visual.transform.SetParent(null, true);
            pool.Return(visual);
        }

        public static ItemModifierBase GetModifier(byte itemId)
        {
            try
            {
                return _itemModifiers[itemId - 1];
            }
            catch (Exception)
            {
                Debug.LogError($"No Item ID registered for {itemId}");
#if UNITY_EDITOR
                Debug.LogError("Check your Resources folder to make sure you have the proper ID set. All IDs must be ascending with no skipping.");
#endif
                throw;
            }
        }

        public static ItemVisualBehavior GetVisualPrefab(byte itemId) => _itemVisualPrefabs[itemId - 1];
    }
}