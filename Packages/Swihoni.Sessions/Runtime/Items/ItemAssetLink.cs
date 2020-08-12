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
        private static Dictionary<byte, ItemModifierBase> _itemModifiers;
        private static Dictionary<byte, Pool<ItemVisualBehavior>> _itemVisualPools;
        private static Dictionary<byte, ItemVisualBehavior> _itemVisualPrefabs;

        public static IEnumerable<ItemModifierBase> ItemVisualPrefabs => _itemModifiers.Values;

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            _itemModifiers = Resources.LoadAll<ItemModifierBase>("Items")
                                      .ToDictionary(modifier => modifier.id, modifier => modifier);
            _itemVisualPrefabs = Resources.LoadAll<GameObject>("Items")
                                          .Select(prefab => prefab.GetComponent<ItemVisualBehavior>())
                                          .Where(visuals => visuals != null && visuals.Id > 0)
                                          .ToDictionary(visuals => visuals.Id, visual => visual);
            _itemVisualPools = _itemVisualPrefabs.Select(pair => new
            {
                id = pair.Key, pool = new Pool<ItemVisualBehavior>(0, () =>
                {
                    ItemVisualBehavior visualsInstance = Object.Instantiate(pair.Value);
                    visualsInstance.name = "Visual";
                    return visualsInstance;
                })
            }).ToDictionary(pair => pair.id, pair => pair.pool);
        }

        public static ItemVisualBehavior ObtainVisuals(byte itemId, PlayerItemAnimatorBehavior playerItemAnimator, in PlayableGraph playerGraph)
        {
            Pool<ItemVisualBehavior> pool = _itemVisualPools[itemId];
            ItemVisualBehavior visual = pool.Obtain();
            visual.SetupForPlayerAnimation(playerItemAnimator, playerGraph);
            return visual;
        }

        public static void ReturnVisuals(ItemVisualBehavior visual)
        {
            Pool<ItemVisualBehavior> pool = _itemVisualPools[visual.Id];
            visual.Cleanup();
            visual.SetActive(false);
            visual.transform.SetParent(null, true);
            pool.Return(visual);
        }

        public static ItemModifierBase GetModifier(byte itemId)
        {
            try
            {
                return _itemModifiers[itemId];
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

        public static ItemVisualBehavior GetVisualPrefab(byte itemId) => _itemVisualPrefabs[itemId];
    }
}