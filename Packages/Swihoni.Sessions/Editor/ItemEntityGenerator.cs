using System.Linq;
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
            // Dictionary<int, GameObject> meshToPrefab = Directory.GetFiles(Application.dataPath, "*.fbx", SearchOption.AllDirectories)
            //                                                     .Select(path => AssetDatabase.LoadAssetAtPath<GameObject>($"Assets{path.Replace(Application.dataPath, string.Empty).Replace("\\", "/")}"))
            //                                                     .SelectMany(gameObject => gameObject.GetComponentsInChildren<MeshFilter>().ToArray())
            //                                                     .ToDictionary(filter => filter.sharedMesh.GetInstanceID(),
            //                                                                   filter => filter.transform.root.gameObject);
            byte modelId = 100 + 1;
            foreach (ItemModifierBase itemBehavior in ItemAssetLink.ItemVisualPrefabs)
            {
                ItemVisualBehavior visualPrefab = ItemAssetLink.GetVisualPrefab(itemBehavior.id);
                // Mesh[] meshes = visualPrefab.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToArray();
                Mesh[] meshes = visualPrefab.GetComponentsInChildren<MeshFilter>().Select(filter => filter.sharedMesh).ToArray();
                // GameObject modelPrefab = meshToPrefab[meshes[0].GetInstanceID()];

                var visualInstance = (GameObject) PrefabUtility.InstantiatePrefab(visualPrefab.gameObject);
                void Recurse(Transform t)
                {
                    foreach (Component c in t.GetComponents<Component>())
                    {
                        if (c is MeshFilter || c is Renderer || c is Transform) continue;
                        Object.DestroyImmediate(c);
                    }
                    foreach (Transform ct in t)
                    {
                        Recurse(ct);
                    }
                }
                Recurse(visualInstance.transform);

                visualInstance.name = $"{itemBehavior.itemName} Entity Visual Variant";
                var visual = visualInstance.AddComponent<EntityVisualBehavior>();
                visual.id = modelId;
                PrefabUtility.SaveAsPrefabAsset(visualInstance, $"Assets/Resources/Entities/Items/{visualInstance.name}.prefab");
                Object.DestroyImmediate(visualInstance);

                Bounds bounds = meshes[0].bounds;
                for (var i = 1; i < meshes.Length; i++)
                    bounds.Encapsulate(meshes[i].bounds);

                var modifierInstance = (GameObject) PrefabUtility.InstantiatePrefab(modifierPrefab);
                modifierInstance.name = $"{itemBehavior.itemName} Entity Modifier Variant";
                modifierInstance.layer = LayerMask.NameToLayer("Item Entity");
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