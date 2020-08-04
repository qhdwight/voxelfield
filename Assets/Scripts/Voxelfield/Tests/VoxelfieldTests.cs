using System.Linq;
using LiteNetLib.Utils;
using NUnit.Framework;
using Swihoni.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels;
using Voxels.Map;

namespace Voxelfield.Tests
{
    public static class VoxelfieldTests
    {
        [Test]
        public static void TestVoxelSerialization()
        {
            var ordered = new OrderedVoxelChangesProperty();
            var change = new VoxelChange
            {
                position = new Position3Int(2, 4, 1),
                magnitude = 5, density = 2, color = new Color32(2, 2, 5, 1), texture = VoxelTexture.Checkered, form = VoxelVolumeForm.Prism,
                natural = true, orientation = 4, replace = true, yaw = 321f, hasBlock = true, isBreakable = true, modifiesBlocks = true,
                noRandom = true, upperBound = new Position3Int(2, 3, 4), revert = true
            };
            ordered.Append(change);

            var writer = new NetDataWriter();
            ordered.Serialize(writer);

            var reader = new NetDataReader(writer.Data);
            var read = new OrderedVoxelChangesProperty();
            read.Deserialize(reader);
            Assert.AreEqual(ordered.List.First(), read.List.First());
        }

        [Test]
        public static void TestMeme()
        {
            // var terrain = new TerrainGenerationComponent
            //     {stoneVoxel = new VoxelChangeProperty(new VoxelChange {texture = VoxelTexture.Checkered, color = new Color32(28, 28, 28, 255)})};
            var map = new MapContainer
            {
                name = new StringProperty("Fort"),
                terrainHeight = new IntProperty(9),
                dimension = new DimensionComponent {lowerBound = new Position3IntProperty(-2, 0, -2), upperBound = new Position3IntProperty(2, 1, 2)},
                terrainGeneration = new TerrainGenerationComponent
                {
                    seed = new IntProperty(1337),
                    octaves = new ByteProperty(3),
                    lateralScale = new FloatProperty(35.0f),
                    verticalScale = new FloatProperty(1.5f),
                    persistence = new FloatProperty(0.5f),
                    lacunarity = new FloatProperty(0.5f),
                    grassVoxel = new VoxelChangeProperty(new VoxelChange {texture = VoxelTexture.Solid, color = new Color32(255, 172, 7, 255)}),
                    stoneVoxel = new VoxelChangeProperty(new VoxelChange {texture = VoxelTexture.Checkered, color = new Color32(28, 28, 28, 255)}),
                },
                models = new ModelsProperty(),
                breakableEdges = new BoolProperty(false)
            };
            map.version.SetTo(Application.version);

            var writer = new NetDataWriter();
            map.Serialize(writer);
            
            var reader = new NetDataReader(writer.Data);
            var read = new TerrainGenerationComponent();
            
            read.Deserialize(reader);
            Assert.AreEqual(0, map.voxelChanges.Count);
        }
    }
}