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

namespace Voxelation.Map
{
    public class MapManager : SingletonBehavior<MapManager>
    {
        private const string MapSaveExtension = "vfm", MapSaveFolder = "Maps";

        private static StringProperty _emptyMapName;
        private static readonly MapContainer EmptyMap = new MapContainer().Zero();
        private static Dictionary<string, TextAsset> _defaultMaps;

#if UNITY_EDITOR
        [SerializeField] private bool m_TruncateDimension = true;
#endif

        private Pool<ModelBehaviorBase>[] m_ModelsPool;
        private StringProperty m_WantedMapName;
        private IEnumerator m_ManageActionsRoutine;

        public static ModelBehaviorBase[] ModelPrefabs { get; private set; }

        public Dictionary<Position3Int, ModelBehaviorBase> Models { get; } = new Dictionary<Position3Int, ModelBehaviorBase>();
        public MapContainer Map { get; private set; } = new MapContainer();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Initialize()
        {
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

        private void Start()
        {
            m_ManageActionsRoutine = ManageActionsRoutine();
            m_WantedMapName = _emptyMapName;
            StartCoroutine(m_ManageActionsRoutine);
            SetupModelPool();
        }

        private void SetupModelPool() =>
            m_ModelsPool = ModelPrefabs
                          .Select(modelBehaviorPrefab => new Pool<ModelBehaviorBase>(0, () =>
                           {
                               ModelBehaviorBase modelInstance = Instantiate(modelBehaviorPrefab);
                               modelInstance.Setup(this);
                               modelInstance.name = modelBehaviorPrefab.name;
                               return modelInstance;
                           }, (modelBehavior, isActive) => modelBehavior.gameObject.SetActive(false))).ToArray();

        private IEnumerator ManageActionsRoutine()
        {
            while (Application.isPlaying)
            {
                yield return Map.name == m_WantedMapName
                    ? null
                    : LoadNamedMap(m_WantedMapName);
            }
        }

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
            string mapPath = $"C:/Users/qhdwi/Projects/Programming/Unity/Compound/Assets/Resources/Maps/{map.name}.bytes";
#endif
            var writer = new NetDataWriter();
            map.Serialize(writer);
            File.WriteAllBytes(mapPath, writer.CopyData());
            Debug.Log($"Saved map to {mapPath}");
            return true;
        }

        public bool SaveCurrentMap(string fileName = null) => SaveMapSave(Map, fileName);

        private IEnumerator LoadNamedMap(StringProperty mapName)
        {
            bool isEmpty = mapName.WithoutValue;
            MapContainer map = isEmpty ? EmptyMap : ReadNamedMap(mapName);
            Debug.Log(isEmpty ? "Unloading map" : $"Starting to load map: {mapName}");

#if UNITY_EDITOR
            if (m_TruncateDimension) map.dimension = new DimensionComponent {lowerBound = new Position3IntProperty(-1, 0, -1), upperBound = new Position3IntProperty(0, 0, 0)};
#endif

            yield return LoadMapSave(map);

            // var transaction = new VoxelChangeTransaction(map.changedVoxels.Count);
            // foreach ((Position3Int position, VoxelChangeData change) in map.changedVoxels)
            //     transaction.AddChange(position, change);
            // transaction.Commit();

            LoadModels(map);

            // PlaceTrees(mapName, mapSave);z

            Debug.Log($"Finished loading map: {mapName}");
            Map = map;
        }

        private void LoadModels(MapContainer map)
        {
            Models.Clear();
            foreach (Pool<ModelBehaviorBase> pool in m_ModelsPool) pool.ReturnAll();
            foreach (KeyValuePair<Position3Int, Container> pair in map.models.Map)
                InstantiateModel(pair.Key, pair.Value);
        }

        private void InstantiateModel(in Position3Int position, Container model)
        {
            ushort modelId = model.Require<ModelIdProperty>();
            ModelBehaviorBase modelInstance = m_ModelsPool[modelId].Obtain();
            modelInstance.Set(position, model);
            modelInstance.transform.SetPositionAndRotation(position - new Vector3 {y = 0.5f}, Quaternion.identity);
            Models.Add(position, modelInstance);
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
        //             mapSave.Models.Add((Position3Int) hit.point, new ModelData {modelId = (ushort) modelId, rotation = rotation});
        //         }
        //         SaveMapSave(mapSave);
        //     }
        // }

        public void SetMap(StringProperty mapName) => m_WantedMapName = mapName;

        public void UnloadMap() => SetMap(_emptyMapName);

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