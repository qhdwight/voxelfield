using System;
using System.Diagnostics;
using System.Linq;
using LiteNetLib.Utils;
using NUnit.Framework;
using Swihoni.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels;
using Voxels.Map;
using Debug = UnityEngine.Debug;

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
            var writeMap = new MapContainer
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
            writeMap.version.SetTo(Application.version);

            var writer = new NetDataWriter();
            writeMap.Serialize(writer);

            var readMap = new MapContainer();

            {
                var reader = new NetDataReader(writer.Data);
                var watch = new Stopwatch();
                watch.Start();
                readMap.Deserialize(reader);
                watch.Stop();
                Debug.Log($"Recursion Elapsed: {watch.ElapsedTicks}");   
            }

            {
                var reader = new NetDataReader(writer.Data);
                var watch = new Stopwatch();
                watch.Start();
                MapContainer element = readMap;
                NetDataReader _reader = reader;
                ((PropertyBase)element[0]).Deserialize(_reader);
                ((PropertyBase)element[1]).Deserialize(_reader);
                ((PropertyBase)element[2]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[3])[0]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[3])[1]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[0]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[1]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[2]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[3]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[4]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[5]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[6]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[7]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[8]).Deserialize(_reader);
                ((PropertyBase)((ComponentBase)element[4])[9]).Deserialize(_reader);
                ((PropertyBase)element[5]).Deserialize(_reader);
                ((PropertyBase)element[6]).Deserialize(_reader);
                ((PropertyBase)element[7]).Deserialize(_reader);
                watch.Stop();
                Debug.Log($"Inline Elapsed: {watch.ElapsedTicks}");   
            }

            ElementExtensions.NavigateZipped((_write, _read) =>
            {
                if (_write is PropertyBase _writeProperty && _read is PropertyBase _readProperty)
                {
                    try
                    {
                        Assert.IsTrue(_writeProperty.Equals(_readProperty));
                    }
                    catch (NotImplementedException)
                    {
                        // ignored
                    }
                }
                return Navigation.Continue;
            }, writeMap, readMap);
            Assert.AreEqual(0, readMap.voxelChanges.Count);
        }
    }
}