﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteNetLib.Utils;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Util.Math;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Voxels.Map
{
    public class MapManager : MonoBehaviour
    {
        private const string MapSaveExtension = "vfm", MapSaveFolder = "Maps";

        private static StringProperty _emptyMapName;
        private static MapContainer _emptyMap;
        private static Dictionary<string, TextAsset> _defaultMaps;

#if UNITY_EDITOR
        [SerializeField] private bool m_TruncateDimension = true;
#endif

        private Pool<ModelBehaviorBase>[] m_ModelsPool;
        private StringProperty m_WantedMapName;
        private uint m_Generation;

        public static ModelBehaviorBase[] ModelPrefabs { get; private set; }

        public Dictionary<Position3Int, ModelBehaviorBase> Models { get; } = new();
        public MapContainer Map { get; private set; } = new();

        public ChunkManager ChunkManager { get; private set; }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
            _emptyMap = new MapContainer().Zero();
            _emptyMapName = new StringProperty();
            _defaultMaps = Resources.LoadAll<TextAsset>("Maps").ToDictionary(mapAsset => mapAsset.name, m => m);
            ModelPrefabs = Resources.LoadAll<ModelBehaviorBase>("Models")
                                    .OrderBy(modifier => modifier.Id).ToArray();
        }

        public static string GetDirectory(string folderName)
        {
            string parentFolder = Directory.GetParent(Application.dataPath).FullName;
            // On Mac the data path is one more folder inside (because everything is packed inside application "file" on Mac which is really a folder)
            if (Application.platform == RuntimePlatform.OSXPlayer) parentFolder = Directory.GetParent(parentFolder).FullName;
            return Path.Combine(parentFolder, folderName);
        }

        private void Awake() => ChunkManager = GetComponentInChildren<ChunkManager>();

        private void Start()
        {
            m_WantedMapName = _emptyMapName;
            StartCoroutine(Runner());
            SetupModelPool();
        }

        private IEnumerator Runner()
        {
            IEnumerator loadMapEnumerator = null;
            while (Application.isPlaying)
            {
                MapLoadingStage stage = ChunkManager.ProgressInfo.stage;
                object current = null;
                try
                {
                    if (stage == MapLoadingStage.Waiting)
                        loadMapEnumerator = LoadNamedMap(m_WantedMapName);
                    if (stage != MapLoadingStage.Failed && loadMapEnumerator != null)
                        if (loadMapEnumerator.MoveNext()) current = loadMapEnumerator.Current;
                        else loadMapEnumerator = null; // Done loading
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Failed to load map: {m_WantedMapName.AsNewString()}: {exception.Message}");
                    ChunkManager.ProgressInfo = new MapProgressInfo {stage = MapLoadingStage.Failed};
                }
                yield return current;
            }
        }

        private void SetupModelPool() =>
            m_ModelsPool = ModelPrefabs
                          .Select(modelBehaviorPrefab => new Pool<ModelBehaviorBase>(0, () =>
                           {
                               ModelBehaviorBase modelInstance = Instantiate(modelBehaviorPrefab);
                               SceneManager.MoveGameObjectToScene(modelInstance.gameObject, gameObject.scene);
                               modelInstance.Setup(this);
                               modelInstance.name = modelBehaviorPrefab.name;
                               return modelInstance;
                           }, (modelBehavior, isActive) => modelBehavior.gameObject.SetActive(false))).ToArray();

        private static string GetMapPath(StringProperty mapName)
            => Path.ChangeExtension(Path.Combine(GetDirectory(MapSaveFolder), mapName.Builder.ToString()), MapSaveExtension);

        private static MapContainer ReadNamedMap(StringProperty mapName)
        {
            byte[] rawMap;
            if (_defaultMaps.TryGetValue(mapName.Builder.ToString(), out TextAsset textAsset))
                rawMap = textAsset.bytes;
            else
            {
                string mapPath = GetMapPath(mapName);
                rawMap = File.ReadAllBytes(mapPath);
            }
            var reader = new NetDataReader(rawMap);
            return new MapContainer().Deserialize(reader);
        }

        public static bool SaveMapSave(MapContainer map, string fileName = null)
        {
            if (fileName != null) map.name.SetTo(fileName);
            map.version.SetTo(Application.version);
#if VOXELFIELD_RELEASE_CLIENT
            string mapPath = GetMapPath(map.name);
#else
#if UNITY_EDITOR
            string mapPath = Path.ChangeExtension(Path.Combine(Application.dataPath, "Resources", "Maps", map.name.AsNewString()), "bytes");
#else
            string mapPath = $@"";
#endif
#endif
            var writer = new NetDataWriter();
            map.Serialize(writer);
            File.WriteAllBytes(mapPath, writer.CopyData());
            Debug.Log($"Saved map to {mapPath}");
#if UNITY_EDITOR
            AssetDatabase.Refresh();
#endif
            return true;
        }

        public bool SaveCurrentMap(string fileName = null) => SaveMapSave(Map, fileName);

        private IEnumerator LoadNamedMap(StringProperty mapName)
        {
            bool isEmpty = mapName.WithoutValue;
            MapContainer map = isEmpty ? _emptyMap : ReadNamedMap(mapName);
            Debug.Log(isEmpty ? "Unloading map" : $"Starting to load map: {mapName}");

#if UNITY_EDITOR
            if (m_TruncateDimension) map.dimension = new DimensionComponent {lowerBound = new Position3IntProperty(-1, 0, -1), upperBound = new Position3IntProperty(0, 0, 0)};
#endif

            // map.terrainGeneration.grassVoxel.Value = new VoxelChange {texture = VoxelTexture.Solid, color = Voxel.Grass};
            // map.terrainGeneration.upperBreakableHeight.Value = 16;

            yield return ChunkManager.LoadMap(map);

            LoadModels(map);

            // PlaceTrees(mapName, mapSave);

            Map = map;

            Debug.Log($"Finished loading map: {mapName}");
        }

        private void LoadModels(MapContainer map)
        {
            Models.Clear();
            foreach (Pool<ModelBehaviorBase> pool in m_ModelsPool) pool.ReturnAll();
            foreach ((Position3Int position, Container model) in map.models.Map)
                InstantiateModel(position, model);
        }

        private void InstantiateModel(in Position3Int position, Container model)
        {
            ushort modelId = model.Require<ModelIdProperty>();
            ModelBehaviorBase modelInstance = m_ModelsPool[modelId].Obtain();
            modelInstance.Set(position, model);
            modelInstance.transform.SetPositionAndRotation(position - new Vector3 {y = 0.5f}, Quaternion.identity);
            Models.Add(position, modelInstance);
        }

        public void SetGeneration(uint generation)
        {
            if (generation > m_Generation)
            {
                ReloadMap();
                m_Generation = generation;
            }
        }

        public void AddModel(in Position3Int position, Container model)
        {
            Map.models.Set(position, model);
            InstantiateModel(position, model);
        }

        public void RemoveModel(in Position3Int position)
        {
            Map.models.Remove(position);
            ModelBehaviorBase model = Models[position];
            Models.Remove(position);
            m_ModelsPool[model.Id].Return(model);
        }

        // private static void PlaceTrees(string mapName, MapSave mapSave)
        // {
        //     if (mapName == "Test")
        //     {
        //         Random.InitState(8);
        //         mapSave.Models = new Dictionary<Position3Int, ModelData>();
        //         for (var i = 0; i < 35; i++)
        //         {
        //             int chunkSize = ChunkManager.Singleton.ChunkSize;
        //             Dimension dimension = mapSave.Dimension;
        //             float x = Random.Range(dimension.lowerBound.x * chunkSize, dimension.upperBound.x * chunkSize),
        //                   z = Random.Range(dimension.lowerBound.x * chunkSize, dimension.upperBound.x * chunkSize);
        //             Physics.Raycast(new Vector3 {x = x, y = 1000.0f, z = z}, Vector3.down, out RaycastHit hit, float.PositiveInfinity);
        //             GameObject[] models = ModelManager.Singleton.Models;
        //             int modelId = Random.Range(0, models.Length);
        //             Quaternion rotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
        //             ModelManager.Singleton.LoadInModel(modelId, hit.point, rotation);
        //             mapSave.Models.Add((Position3Int) hit.point, new MoEdelData {modelId = (ushort) modelId, rotation = rotation});
        //         }
        //         SaveMapSave(mapSave);
        //     }
        // }

        public void ReloadMap() => ChunkManager.ProgressInfo = new MapProgressInfo {stage = MapLoadingStage.Waiting};

        public bool SetNamedMap(StringProperty mapName)
        {
            if (m_WantedMapName == mapName) return false;

            m_WantedMapName = mapName;
            ReloadMap();
            return true;
        }

        public void SetMapToUnloaded() => SetNamedMap(_emptyMapName);
    }
}