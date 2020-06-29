using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Voxel.Map
{
    public class MapManager : SingletonBehavior<MapManager>
    {
        private const string MapSaveExtension = "vfm", MapSaveFolder = "Maps";

        private static Dictionary<string, TextAsset> _defaultMaps;
        private static readonly StringProperty TestMapName = new StringProperty("Test"), EmptyMapName = new StringProperty();
        private static readonly MapContainer EmptyMap = new MapContainer().Zero();

        private StringProperty m_WantedMapName = EmptyMapName;
        private IEnumerator m_ManageActionsRoutine;

        public MapContainer Map { get; private set; } = new MapContainer();

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize() => _defaultMaps = Resources.LoadAll<TextAsset>("Maps").ToDictionary(mapAsset => mapAsset.name, m => m);

        public static string GetDirectory(string folderName)
        {
            string parentFolder = Directory.GetParent(Application.dataPath).FullName;
            // On Mac the data path is one more folder inside (because everything is packed inside application "file" on Mac which is really a folder)
            if (Application.platform == RuntimePlatform.OSXPlayer) parentFolder = Directory.GetParent(parentFolder).FullName;
            return Path.Combine(parentFolder, folderName);
        }

        private void Start()
        {
            SaveTestMap();
            m_ManageActionsRoutine = ManageActionsRoutine();
            StartCoroutine(m_ManageActionsRoutine);
            SetMap(EmptyMapName);
        }

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

        private static bool SaveMapSave(MapContainer map)
        {
            string mapPath = GetMapPath(map.name);
#if UNITY_EDITOR
            if (map.name == TestMapName && Application.isEditor) mapPath = "Assets/Resources/Maps/Test.bytes";
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

            if (Application.isEditor)
            {
                map.dimension = new DimensionComponent {lowerBound = new Position3IntProperty(-1, 0, -1), upperBound = new Position3IntProperty(0, 0, 0)};
            }
            // mapSave.Models.Add(new Position3Int {x = -10, y = 20}, new ModelData {modelId = ModelData.Spawn, spawnTeam = 0, rotation = Quaternion.identity});
            // mapSave.Models.Add(new Position3Int {y = 20}, new ModelData {modelId = ModelData.Spawn, spawnTeam = 1, rotation = Quaternion.identity});
            // mapSave.Models.Add(new Position3Int {x = 10, y = 20}, new ModelData {modelId = ModelData.Spawn, spawnTeam = 2, rotation = Quaternion.identity});
            // mapSave.Models.Add(new Position3Int {x = 20, y = 20}, new ModelData {modelId = ModelData.Spawn, spawnTeam = 3, rotation = Quaternion.identity});

            yield return LoadMapSave(map);

            var transaction = new VoxelChangeTransaction();
            foreach ((Position3Int position, VoxelChangeData change) in map.changedVoxels)
                transaction.AddChange(position, change);
            transaction.Commit();

            // PlaceTrees(mapName, mapSave);

            Debug.Log($"Finished loading map: {mapName}");
            Map = map;
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

        [Conditional("UNITY_EDITOR")]
        private static void SaveTestMap()
        {
            var testMap = new MapContainer
            {
                name = new StringProperty("Test"),
                terrainHeight = new IntProperty(4),
                dimension = new DimensionComponent {lowerBound = new Position3IntProperty(-1, -1, -1), upperBound = new Position3IntProperty(1, 1, 1)},
                noise = new NoiseComponent
                {
                    seed = new IntProperty(0),
                    octaves = new ByteProperty(4),
                    lateralScale = new FloatProperty(35.0f),
                    verticalScale = new FloatProperty(3.5f),
                    persistance = new FloatProperty(0.5f),
                    lacunarity = new FloatProperty(0.5f)
                }
            };
            SaveMapSave(testMap);
        }

        public void SetMap(StringProperty mapName) => m_WantedMapName = mapName;

        public void UnloadMap() => m_WantedMapName = EmptyMapName;

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