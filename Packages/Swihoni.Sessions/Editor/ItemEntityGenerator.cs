using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Items.Visuals;
using UnityEditor;
using UnityEngine;

namespace Swihoni.Sessions.Editor
{
    public static class ItemEntityGenerator
    {
        [MenuItem("Session/Generate Item Entities")]
        private static void Generate()
        {
            ItemAssetLink.Initialize();

            var modifierPrefab = Resources.Load<GameObject>("Entities/Item Entity Modifier");

            byte modelId = 100;
            foreach (ItemModifierBase itemBehavior in ItemAssetLink.ItemVisualPrefabs)
            {
                ItemVisualBehavior visualPrefab = ItemAssetLink.GetVisualPrefab(itemBehavior.id);
                Renderer[] renderers = visualPrefab.GetComponentsInChildren<Renderer>();
                GameObject modelPrefab = PrefabUtility.GetCorrespondingObjectFromOriginalSource(renderers[0]).transform.root.gameObject;

                var visualInstance = (GameObject) PrefabUtility.InstantiatePrefab(modelPrefab);
                visualInstance.name = $"{itemBehavior.itemName} Entity Visual Variant";
                var visual = visualInstance.AddComponent<EntityVisualBehavior>();
                visual.id = modelId;
                PrefabUtility.SaveAsPrefabAsset(visualInstance, $"Assets/Resources/Entities/Items/{visualInstance.name}.prefab");
                Object.DestroyImmediate(visualInstance);
                
                Bounds bounds = renderers[0].bounds;
                for (var i = 1; i < renderers.Length; i++)
                    bounds.Encapsulate(renderers[i].bounds);
                
                var modifierInstance = (GameObject) PrefabUtility.InstantiatePrefab(modifierPrefab);
                modifierInstance.name = $"{itemBehavior.itemName} Entity Modifier Variant";
                var modifier = modifierInstance.GetComponent<ItemEntityModifierBehavior>();
                modifier.id = modelId;
                var collider = modifierInstance.GetComponent<BoxCollider>();
                collider.center = bounds.center;
                collider.size = bounds.size;
                PrefabUtility.SaveAsPrefabAsset(modifierInstance, $"Assets/Resources/Entities/Items/{modifierInstance.name}.prefab");
                Object.DestroyImmediate(modifierInstance);

                modelId++;
            }
        }
    }
}