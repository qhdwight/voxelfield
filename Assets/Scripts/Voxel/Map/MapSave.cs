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
        public byte? spawnTeam;

        public static void Serialize(NetDataWriter writer, ModelData model)
        {
            writer.Put(model.modelId);
            writer.Put(model.rotation.x);
            writer.Put(model.rotation.y);
            writer.Put(model.rotation.z);
            writer.Put(model.rotation.w);
            writer.Put(model.spawnTeam.HasValue);
            if (model.spawnTeam is byte team) writer.Put(team);
        }

        public static ModelData Deserialize(NetDataReader reader)
        {
            var modelData = new ModelData
            {
                modelId = reader.GetUShort(),
                rotation = new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat()),
            };
            if (reader.GetBool()) modelData.spawnTeam = reader.GetByte();
            return modelData;
        }
    }

    public class MapSave
    {
        public string Name { get; set; }
        public int TerrainHeight { get; set; }
        public Dimension Dimension { get; set; }
        public Dictionary<Position3Int, BrushStroke> BrushStrokes { get; set; } = new Dictionary<Position3Int, BrushStroke>();
        public Dictionary<Position3Int, VoxelChangeData> ChangedVoxels { get; set; } = new Dictionary<Position3Int, VoxelChangeData>();
        public Dictionary<Position3Int, ModelData> Models { get; set; } = new Dictionary<Position3Int, ModelData>();
        public bool DynamicChunkLoading { get; set; }
        public NoiseData? TerrainGenerationData { get; set; }

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
            var mapSave = new MapSave
            {
                Name = reader.GetString(),
                TerrainHeight = reader.GetInt(),
                Dimension = Dimension.Deserialize(reader),
                DynamicChunkLoading = reader.GetBool()
            };
            // Noise data
            bool hasTerrainGenerationData = reader.GetBool();
            NoiseData? data = hasTerrainGenerationData ? (NoiseData?) NoiseData.Deserialize(reader) : null;
            mapSave.TerrainGenerationData = data;
            // Brush strokes
            int brushStrokeCount = reader.GetInt();
            for (var _ = 0; _ < brushStrokeCount; _++)
                mapSave.BrushStrokes.Add(Position3Int.Deserialize(reader), BrushStroke.Deserialize(reader));
            // Changed voxels
            int voxelChangeCount = reader.GetInt();
            for (var _ = 0; _ < voxelChangeCount; _++)
                mapSave.ChangedVoxels.Add(Position3Int.Deserialize(reader), VoxelChangeData.Deserialize(reader));
            int modelCount = reader.GetInt();
            for (var _ = 0; _ < modelCount; _++)
                mapSave.Models.Add(Position3Int.Deserialize(reader), ModelData.Deserialize(reader));
            return mapSave;
        }
    }
}