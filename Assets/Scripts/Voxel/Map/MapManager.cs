using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteNetLib.Utils;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;

namespace Voxel.Map
{
    public class MapManager : SingletonBehavior<MapManager>
    {
        private const string MapSaveExtension = "vfm", MapSaveFolder = "Maps";

        private static readonly StringProperty TestMapName = new StringProperty("Test"), EmptyMapName = new StringProperty();
        private static readonly MapContainer EmptyMap = new MapContainer().Zero();
        private static Dictionary<string, TextAsset> _defaultMaps;
        private static ModelBehavior[] _modelPrefabs;
        private Pool<ModelBehavior>[] m_ModelsPool;

        private StringProperty m_WantedMapName = EmptyMapName;
        private IEnumerator m_ManageActionsRoutine;

        public Dictionary<Position3Int, ModelBehavior> Models { get; } = new Dictionary<Position3Int, ModelBehavior>();
        public MapContainer Map { get; private set; } = new MapContainer();

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize()
        {
            _defaultMaps = Resources.LoadAll<TextAsset>("Maps").ToDictionary(mapAsset => mapAsset.name, m => m);
            _modelPrefabs = Resources.LoadAll<ModelBehavior>("Models")
                                     .OrderBy(modifier => modifier.Id).ToArray();
        }

        public static string GetDirectory(string folderName)
        {
            string parentFolder = Directory.GetParent(Application.dataPath).FullName;
            // On Mac the data path is one more folder inside (because everything is packed inside application "file" on Mac which is really a folder)
            if (Application.platform == RuntimePlatform.OSXPlayer) parentFolder = Directory.GetParent(parentFolder).FullName;
            return Path.Combine(parentFolder, folderName);
        }

        private void Start()
        {
            m_ManageActionsRoutine = ManageActionsRoutine();
            StartCoroutine(m_ManageActionsRoutine);
            UnloadMap();
            SetupModelPool();
        }

        private void SetupModelPool() =>
            m_ModelsPool = _modelPrefabs
                          .Select(modelBehaviorPrefab => new Pool<ModelBehavior>(0, () =>
                           {
                               ModelBehavior modelInstance = Instantiate(modelBehaviorPrefab);
                               modelInstance.Setup(this);
                               modelInstance.name = modelBehaviorPrefab.name;
                               return modelInstance;
                           }, (modelBehavior, isActive) => modelBehavior.gameObject.SetActive(isActive))).ToArray();

        private IEnumerator ManageActionsRoutine()
        {
            while (Application.isPlaying)
            {
                yield return Map.name == m_WantedMapName
                    ? null
                    : LoadMap(m_WantedMapName);
            }
        }

        private static string GetMapPath(StringProperty mapName) => Path.ChangeExtension(Path.Combine(GetDirectory(MapSaveFolder), mapName.Builder.ToString()), MapSaveExtension);

        private static MapContainer ReadMapSave(StringProperty mapName)
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
            var map = new MapContainer();
            map.Deserialize(reader);
            return map;
        }

        public static bool SaveMapSave(MapContainer map)
        {
            string mapPath = GetMapPath(map.name);
#if UNITY_EDITOR
            if (map.name == TestMapName) mapPath = "Assets/Resources/Maps/Test.bytes";
#endif
            var writer = new NetDataWriter();
            map.Serialize(writer);
            File.WriteAllBytes(mapPath, writer.CopyData());
            return true;
        }

        public bool SaveCurrentMap() => SaveMapSave(Map);

        private IEnumerator LoadMap(StringProperty mapName)
        {
            Debug.Log($"Starting to load map: {mapName}");
            MapContainer map = mapName.WithoutValue ? EmptyMap : ReadMapSave(mapName);

#if UNITY_EDITOR
            map.dimension = new DimensionComponent {lowerBound = new Position3IntProperty(-1, 0, -1), upperBound = new Position3IntProperty(0, 0, 0)};
#endif

            yield return LoadMapSave(map);

            var transaction = new VoxelChangeTransaction();
            foreach ((Position3Int position, VoxelChangeData change) in map.changedVoxels)
                transaction.AddChange(position, change);
            transaction.Commit();

            LoadModels(map);

            // PlaceTrees(mapName, mapSave);

            Debug.Log($"Finished loading map: {mapName}");
            Map = map;
        }

        private void LoadModels(MapContainer map)
        {
            Models.Clear();
            foreach (Pool<ModelBehavior> pool in m_ModelsPool) pool.ReturnAll();
            foreach ((Position3Int position, Container model) in map.models)
            {
                ushort modelId = model.Require<ModelIdProperty>();
                ModelBehavior modelInstance = m_ModelsPool[modelId].Obtain();
                modelInstance.SetContainer(model);
                modelInstance.transform.SetPositionAndRotation(position + new Vector3 {y = 0.5f}, Quaternion.identity);
                Models.Add(position, modelInstance);
            }
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
        //             mapSave.Models.Add((Position3Int) hit.point, new ModelData {modelId = (ushort) modelId, rotation = rotation});
        //         }
        //         SaveMapSave(mapSave);
        //     }
        // }

        public void SetMap(StringProperty mapName) => m_WantedMapName = mapName;

        public void UnloadMap() => SetMap(EmptyMapName);

        private static IEnumerator LoadMapSave(MapContainer save)
        {
//        if (!save.DynamicChunkLoading)
//        {    
//            LoadingScreen.Singleton.SetInterfaceActive(true);
//            LoadingScreen.Singleton.SetProgress(0.0f);
//        }
            yield return ChunkManager.Singleton.LoadMap(save);
        }
    }
}