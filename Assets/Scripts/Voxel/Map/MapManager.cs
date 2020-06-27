using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using LiteNetLib.Utils;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;

namespace Voxel.Map
{
    public class MapManager : SingletonBehavior<MapManager>
    {
        private const string MapSaveExtension = "vfm", MapSaveFolder = "Maps";

        private string m_WantedMap;

        private static Dictionary<string, TextAsset> _defaultMaps;

        public MapSave Map { get; private set; }

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
            // SaveTestMap();
            StartCoroutine(ManageActionsRoutine());
            SetMap("Menu");
        }

        private IEnumerator ManageActionsRoutine()
        {
            while (Application.isPlaying)
            {
                yield return m_WantedMap == null || Map != null && m_WantedMap == Map.Name
                    ? null
                    : LoadMap(m_WantedMap);
            }
        }

        private static string GetMapPath(string mapName) => Path.ChangeExtension(Path.Combine(GetDirectory(MapSaveFolder), mapName), MapSaveExtension);

        private static MapSave ReadMapSave(string mapName)
        {
            byte[] mapData;
            if (_defaultMaps.TryGetValue(mapName, out TextAsset textAsset))
                mapData = textAsset.bytes;
            else
            {
                string mapPath = GetMapPath(mapName);
                mapData = File.ReadAllBytes(mapPath);
            }
            var reader = new NetDataReader(mapData);
            MapSave mapSave = MapSave.Deserialize(reader);
            return mapSave;
        }

        private static bool SaveMapSave(MapSave mapSave)
        {
            string mapPath = GetMapPath(mapSave.Name);
            if (mapSave.Name == "Test" && Application.isEditor) mapPath = "Assets/Resources/Maps/Test.bytes";
            var writer = new NetDataWriter();
            MapSave.Serialize(mapSave, writer);
            File.WriteAllBytes(mapPath, writer.CopyData());
            return true;
        }

        public bool SaveCurrentMap() => SaveMapSave(Map);

        private IEnumerator LoadMap(string mapName)
        {
            Debug.Log($"Starting to load map: {mapName}");
            MapSave mapSave = mapName == "Menu" ? new MapSave {Name = mapName} : ReadMapSave(mapName);

            ModelManager.Singleton.ClearAllModels();
            foreach (KeyValuePair<Position3Int, ModelData> model in mapSave.Models)
                ModelManager.Singleton.LoadInModel(model.Value.modelId, model.Key, model.Value.rotation);

            if (Application.isEditor) mapSave.Dimension = new Dimension(new Position3Int(-1, 0, -1), new Position3Int(0, 0, 0));

            yield return LoadMapSave(mapSave);

            foreach (KeyValuePair<Position3Int, VoxelChangeData> changed in mapSave.ChangedVoxels)
                ChunkManager.Singleton.SetVoxelData(changed.Key, changed.Value);

            // PlaceTrees(mapName, mapSave);

            Debug.Log($"Finished loading map: {mapName}");
            Map = mapSave;
        }

        private static void PlaceTrees(string mapName, MapSave mapSave)
        {
            if (mapName == "Test")
            {
                Random.InitState(8);
                mapSave.Models = new Dictionary<Position3Int, ModelData>();
                for (var i = 0; i < 35; i++)
                {
                    int chunkSize = ChunkManager.Singleton.ChunkSize;
                    Dimension dimension = mapSave.Dimension;
                    float x = Random.Range(dimension.lowerBound.x * chunkSize, dimension.upperBound.x * chunkSize),
                          z = Random.Range(dimension.lowerBound.x * chunkSize, dimension.upperBound.x * chunkSize);
                    Physics.Raycast(new Vector3 {x = x, y = 1000.0f, z = z}, Vector3.down, out RaycastHit hit, float.PositiveInfinity);
                    GameObject[] models = ModelManager.Singleton.Models;
                    int modelId = Random.Range(0, models.Length);
                    Quaternion rotation = Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f);
                    ModelManager.Singleton.LoadInModel(modelId, hit.point, rotation);
                    mapSave.Models.Add((Position3Int) hit.point, new ModelData {modelId = (ushort) modelId, rotation = rotation});
                }
                SaveMapSave(mapSave);
            }
        }

        private static void SaveTestMap()
        {
            var testMap = new MapSave
            {
                Name = "Test",
                TerrainHeight = 4,
                Dimension = new Dimension(new Position3Int(-1, -1, -1), new Position3Int(1, 1, 1)),
                TerrainGenerationData = new NoiseData
                {
                    seed = 0,
                    octaves = 4,
                    lateralScale = 35.0f,
                    verticalScale = 3.5f,
                    persistance = 0.5f,
                    lacunarity = 0.5f
                }
            };
            SaveMapSave(testMap);
        }

        public void SetMap(string mapName) => m_WantedMap = mapName;

        private static IEnumerator LoadMapSave(MapSave save)
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