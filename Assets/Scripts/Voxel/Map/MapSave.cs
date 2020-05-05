using System.Collections.Generic;
using System.IO;
using Swihoni.Util.Math;
using UnityEngine;

namespace Voxel.Map
{
    public struct ModelData
    {
        public ushort modelId;
        public Quaternion rotation;

        public static void Serialize(BinaryWriter message, ModelData model)
        {
            message.Write(model.modelId);
            message.Write(model.rotation.x);
            message.Write(model.rotation.y);
            message.Write(model.rotation.z);
            message.Write(model.rotation.w);
        }

        public static ModelData Deserialize(BinaryReader message)
        {
            return new ModelData
            {
                modelId = message.ReadUInt16(),
                rotation = new Quaternion(message.ReadSingle(), message.ReadSingle(), message.ReadSingle(), message.ReadSingle())
            };
        }
    }

    public class MapSave
    {
        public MapSave(string name, int terrainHeight, Dimension dimension, Dictionary<Position3Int, BrushStroke> brushStrokes,
                       Dictionary<Position3Int, VoxelChangeData> changedVoxels, bool dynamicChunkLoading, NoiseData? terrainGenerationData,
                       Dictionary<Position3Int, ModelData> models)
        {
            Name = name;
            TerrainHeight = terrainHeight;
            Dimension = dimension;
            BrushStrokes = brushStrokes;
            ChangedVoxels = changedVoxels;
            DynamicChunkLoading = dynamicChunkLoading;
            TerrainGenerationData = terrainGenerationData;
            Models = models;
        }

        public string Name { get; }
        public int TerrainHeight { get; }
        public Dimension Dimension { get; }
        public Dictionary<Position3Int, BrushStroke> BrushStrokes { get; }
        public Dictionary<Position3Int, VoxelChangeData> ChangedVoxels { get; }
        public Dictionary<Position3Int, ModelData> Models { get; }
        public bool DynamicChunkLoading { get; }
        public NoiseData? TerrainGenerationData { get; }

        public static void Serialize(MapSave save, BinaryWriter message)
        {
            message.Write(save.Name);
            message.Write(save.TerrainHeight);
            Dimension.Serialize(save.Dimension, message);
            message.Write(save.DynamicChunkLoading);
            // Noise data
            message.Write(save.TerrainGenerationData.HasValue);
            if (save.TerrainGenerationData.HasValue)
                NoiseData.Serialize(save.TerrainGenerationData.Value, message);
            // Brush strokes
            message.Write(save.BrushStrokes.Count);
            foreach (KeyValuePair<Position3Int, BrushStroke> brushStroke in save.BrushStrokes)
            {
                Position3Int.Serialize(brushStroke.Key, message);
                BrushStroke.Serialize(brushStroke.Value, message);
            }
            // Changed voxels
            message.Write(save.ChangedVoxels.Count);
            foreach (KeyValuePair<Position3Int, VoxelChangeData> change in save.ChangedVoxels)
            {
                Position3Int.Serialize(change.Key, message);
                VoxelChangeData.Serialize(message, change.Value);
            }
            message.Write(save.Models.Count);
            foreach (KeyValuePair<Position3Int, ModelData> model in save.Models)
            {
                Position3Int.Serialize(model.Key, message);
                ModelData.Serialize(message, model.Value);
            }
        }

        public static MapSave Deserialize(BinaryReader message)
        {
            string name = message.ReadString();
            int terrainHeight = message.ReadInt32();
            Dimension dimension = Dimension.Deserialize(message);
            bool dynamic = message.ReadBoolean();
            // Noise data
            bool hasTerrainGenerationData = message.ReadBoolean();
            NoiseData? data = hasTerrainGenerationData ? (NoiseData?) NoiseData.Deserialize(message) : null;
            // Brush strokes
            int brushStrokeCount = message.ReadInt32();
            var strokes = new Dictionary<Position3Int, BrushStroke>(brushStrokeCount);
            for (var _ = 0; _ < brushStrokeCount; _++)
                strokes.Add(Position3Int.Deserialize(message), BrushStroke.Deserialize(message));
            // Changed voxels
            int voxelChangeCount = message.ReadInt32();
            var changeData = new Dictionary<Position3Int, VoxelChangeData>(voxelChangeCount);
            for (var _ = 0; _ < voxelChangeCount; _++)
                changeData.Add(Position3Int.Deserialize(message), VoxelChangeData.Deserialize(message));
            int modelCount = message.ReadInt32();
            var models = new Dictionary<Position3Int, ModelData>(modelCount);
            for (var _ = 0; _ < modelCount; _++)
                models.Add(Position3Int.Deserialize(message), ModelData.Deserialize(message));
            return new MapSave(name, terrainHeight, dimension, strokes, changeData, dynamic, data, models);
        }
    }
}