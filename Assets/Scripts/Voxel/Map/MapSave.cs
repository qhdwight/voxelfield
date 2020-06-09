using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Util.Math;
using UnityEngine;

namespace Voxel.Map
{
    public struct ModelData
    {
        public ushort modelId;
        public Quaternion rotation;

        public static void Serialize(NetDataWriter writer, ModelData model)
        {
            writer.Put(model.modelId);
            writer.Put(model.rotation.x);
            writer.Put(model.rotation.y);
            writer.Put(model.rotation.z);
            writer.Put(model.rotation.w);
        }

        public static ModelData Deserialize(NetDataReader reader)
        {
            return new ModelData
            {
                modelId = reader.GetUShort(),
                rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat())
            };
        }
    }

    public class MapSave
    {
        public MapSave(string name, int terrainHeight = 0, Dimension dimension = new Dimension(), Dictionary<Position3Int, BrushStroke> brushStrokes = null,
                       Dictionary<Position3Int, VoxelChangeData> changedVoxels = null, bool dynamicChunkLoading = false, NoiseData? terrainGenerationData = null,
                       Dictionary<Position3Int, ModelData> models = null)
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
        public Dimension Dimension { get; set; }
        public Dictionary<Position3Int, BrushStroke> BrushStrokes { get; }
        public Dictionary<Position3Int, VoxelChangeData> ChangedVoxels { get; }
        public Dictionary<Position3Int, ModelData> Models { get; }
        public bool DynamicChunkLoading { get; }
        public NoiseData? TerrainGenerationData { get; }

        public static void Serialize(MapSave save, NetDataWriter writer)
        {
            writer.Put(save.Name);
            writer.Put(save.TerrainHeight);
            Dimension.Serialize(save.Dimension, writer);
            writer.Put(save.DynamicChunkLoading);
            // Noise data
            writer.Put(save.TerrainGenerationData.HasValue);
            if (save.TerrainGenerationData.HasValue)
                NoiseData.Serialize(save.TerrainGenerationData.Value, writer);
            // Brush strokes
            writer.Put(save.BrushStrokes.Count);
            foreach (KeyValuePair<Position3Int, BrushStroke> brushStroke in save.BrushStrokes)
            {
                Position3Int.Serialize(brushStroke.Key, writer);
                BrushStroke.Serialize(brushStroke.Value, writer);
            }
            // Changed voxels
            writer.Put(save.ChangedVoxels.Count);
            foreach (KeyValuePair<Position3Int, VoxelChangeData> change in save.ChangedVoxels)
            {
                Position3Int.Serialize(change.Key, writer);
                VoxelChangeData.Serialize(writer, change.Value);
            }
            writer.Put(save.Models.Count);
            foreach (KeyValuePair<Position3Int, ModelData> model in save.Models)
            {
                Position3Int.Serialize(model.Key, writer);
                ModelData.Serialize(writer, model.Value);
            }
        }

        public static MapSave Deserialize(NetDataReader reader)
        {
            string name = reader.GetString();
            int terrainHeight = reader.GetInt();
            Dimension dimension = Dimension.Deserialize(reader);
            bool dynamic = reader.GetBool();
            // Noise data
            bool hasTerrainGenerationData = reader.GetBool();
            NoiseData? data = hasTerrainGenerationData ? (NoiseData?) NoiseData.Deserialize(reader) : null;
            // Brush strokes
            int brushStrokeCount = reader.GetInt();
            var strokes = new Dictionary<Position3Int, BrushStroke>(brushStrokeCount);
            for (var _ = 0; _ < brushStrokeCount; _++)
                strokes.Add(Position3Int.Deserialize(reader), BrushStroke.Deserialize(reader));
            // Changed voxels
            int voxelChangeCount = reader.GetInt();
            var changeData = new Dictionary<Position3Int, VoxelChangeData>(voxelChangeCount);
            for (var _ = 0; _ < voxelChangeCount; _++)
                changeData.Add(Position3Int.Deserialize(reader), VoxelChangeData.Deserialize(reader));
            int modelCount = reader.GetInt();
            var models = new Dictionary<Position3Int, ModelData>(modelCount);
            for (var _ = 0; _ < modelCount; _++)
                models.Add(Position3Int.Deserialize(reader), ModelData.Deserialize(reader));
            return new MapSave(name, terrainHeight, dimension, strokes, changeData, dynamic, data, models);
        }
    }
}