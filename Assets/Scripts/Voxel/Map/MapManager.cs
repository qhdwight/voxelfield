using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEditor;
using UnityEngine;

namespace Voxel.Map
{
    public class MapAsset : TextAsset
    {
        private MapAsset(string text) : base(text) { }

        public static MapAsset New(MapSave mapSave)
        {
            using (var stream = new MemoryStream())
            using (var writer = new BinaryWriter(stream))
            {
                MapSave.Serialize(mapSave, writer);
                var rawText = BitConverter.ToString(stream.GetBuffer(), 0, (int) stream.Position);
                return new MapAsset(rawText);
            }
        }
    }

    public class MapManager : SingletonBehavior<MapManager>
    {
        private const string MapSaveExtension = "vfm", MapSaveFolder = "Maps";

        private readonly Queue<LoadMapAction> m_MapActions = new Queue<LoadMapAction>();

        private static Dictionary<string, TextAsset> _defaultMaps;

        public MapSave Map { get; private set; }

        private readonly struct LoadMapAction
        {
            public readonly string mapName;

            public LoadMapAction(string mapName) => this.mapName = mapName;

            public IEnumerator Execute() { yield return Singleton.LoadMap(mapName); }
        }

        [RuntimeInitializeOnLoadMethod]
        public static void Initialize() { _defaultMaps = Resources.LoadAll<TextAsset>("Maps").ToDictionary(mapAsset => mapAsset.name, m => m); }

        public static string GetDirectory(string folderName)
        {
            string parentFolder = Directory.GetParent(Application.dataPath).FullName;
            // On Mac the data path is one more folder inside (because everything is packed inside application "file" on Mac which is really a folder)
            if (Application.platform == RuntimePlatform.OSXPlayer) parentFolder = Directory.GetParent(parentFolder).FullName;
            return Path.Combine(parentFolder, folderName);
        }

        private void Start()
        {
            ChunkManager.Singleton.MapProgress += HandleMapProgressInfo;
            StartCoroutine(ManageActionsRoutine());
            SetMap("Menu");
        }

        private IEnumerator ManageActionsRoutine()
        {
            // TODO do a different way?
            while (Application.isPlaying)
                yield return m_MapActions.Count > 0 ? m_MapActions.Dequeue().Execute() : null;
        }

        private static string GetMapPath(string mapName) => Path.ChangeExtension(Path.Combine(GetDirectory(MapSaveFolder), mapName), MapSaveExtension);

        private static MapSave ReadMapSave(string mapName)
        {
            string mapPath = GetMapPath(mapName);
            Stream mapStream = _defaultMaps.TryGetValue(mapName, out TextAsset textAsset)
                ? (Stream) new MemoryStream(textAsset.bytes)
                : File.OpenRead(mapPath);
            MapSave mapSave;
            using (var reader = new BinaryReader(mapStream))
                mapSave = MapSave.Deserialize(reader);
            mapStream.Dispose();
            return mapSave;
        }

        private static bool SaveMapSave(MapSave mapSave)
        {
            string mapPath = GetMapPath(mapSave.Name);
            using (FileStream file = File.OpenWrite(mapPath))
            using (var writer = new BinaryWriter(file))
            {
                MapSave.Serialize(mapSave, writer);
            }
            AssetDatabase.CreateAsset(MapAsset.New(mapSave), "Assets/Test.bytes");
            return true;
        }

        public bool SaveCurrentMap() => SaveMapSave(Map);

        private IEnumerator LoadMap(string mapName)
        {
            Debug.Log($"Starting to load map: {mapName}");
            MapSave mapSave = mapName == "Menu" ? new MapSave(mapName) : ReadMapSave(mapName);
            ModelManager.Singleton.ClearAllModels();
            if (mapSave.Models != null)
                foreach (KeyValuePair<Position3Int, ModelData> model in mapSave.Models)
                    ModelManager.Singleton.LoadInModel(model.Value.modelId, model.Key, model.Value.rotation);
            yield return SetMapSave(mapSave);
            // if (mapName == "Test")
            // {
            //     Random.InitState(8);
            //     for (var i = 0; i < 35; i++)
            //     {
            //         float x = Random.Range(-24.0f, 24.0f), z = Random.Range(-24.0f, 24.0f);
            //         Physics.Raycast(new Vector3 {x = x, y = 1000.0f, z = z}, Vector3.down, out RaycastHit hit, float.PositiveInfinity);
            //         GameObject[] models = ModelManager.Singleton.Models;
            //         int modelId = Random.Range(0, models.Length);
            //         ModelManager.Singleton.LoadInModel(modelId, hit.point,
            //                                            Quaternion.Euler(0.0f, Random.Range(0.0f, 360.0f), 0.0f));
            //         mapSave.Models.Add((Position3Int) hit.point, new ModelData {modelId = (ushort) modelId, rotation = Quaternion.identity});
            //     }
            //     SaveMapSave(mapSave);
            // }
            Debug.Log($"Finished loading map: {mapName}");
            Map = mapSave;
        }

        private static void SaveTestMap()
        {
            var testMap = new MapSave("Test", 4,
                                      new Dimension(new Position3Int(-2, -1, -2), new Position3Int(2, 1, 2)),
                                      new Dictionary<Position3Int, BrushStroke>(), new Dictionary<Position3Int, VoxelChangeData>(),
                                      false,
                                      new NoiseData
                                      {
                                          seed = 0,
                                          octaves = 4,
                                          lateralScale = 35.0f,
                                          verticalScale = 3.5f,
                                          persistance = 0.5f,
                                          lacunarity = 0.5f
                                      }, new Dictionary<Position3Int, ModelData>());
            SaveMapSave(testMap);
        }

        public void SetMap(string mapName)
        {
            if (ChunkManager.Singleton.Map != null && ChunkManager.Singleton.Map.Name == mapName) return;
            m_MapActions.Enqueue(new LoadMapAction(mapName));
        }

        private static IEnumerator SetMapSave(MapSave save)
        {
//        if (!save.DynamicChunkLoading)
//        {    
//            LoadingScreen.Singleton.SetInterfaceActive(true);
//            LoadingScreen.Singleton.SetProgress(0.0f);
//        }
            yield return ChunkManager.Singleton.SetMap(save);
        }

        private void HandleMapProgressInfo(in MapProgressInfo mapProgress)
        {
//            Debug.Log($"Map Progress: Stage: {mapProgress.stage}, Percent Complete: {mapProgress.progress:P1}");
//        switch (mapProgress.stage)
//        {
//            case MapLoadingStage.CLEANING_UP:
//                LoadingScreen.Singleton.SetLoadingText("Cleaning up previouis chunks...");
//                break;
//            case MapLoadingStage.SETTING_UP:
//                LoadingScreen.Singleton.SetLoadingText("Setting up chunks...");
//                break;
//            case MapLoadingStage.GENERATING:
//                LoadingScreen.Singleton.SetLoadingText("Generating chunks from save...");
//                break;
//            case MapLoadingStage.UPDATING_MESH:
//                LoadingScreen.Singleton.SetLoadingText("Rendering chunk meshes...");
//                break;
//            case MapLoadingStage.COMPLETED:
//                LoadingScreen.Singleton.SetInterfaceActive(false);
//                break;
//        }
//        LoadingScreen.Singleton.SetProgress(mapProgress.progress);
        }
    }
}