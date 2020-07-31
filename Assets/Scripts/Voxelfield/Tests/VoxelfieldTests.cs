using System.Linq;
using LiteNetLib.Utils;
using NUnit.Framework;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels;

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
                noRandom = true, upperBound = new Position3Int(2, 3, 4)
            };
            ordered.Add(change);
            
            var writer = new NetDataWriter();
            ordered.Serialize(writer);
            
            var reader = new NetDataReader(writer.Data);
            var meme = new OrderedVoxelChangesProperty();
            meme.Deserialize(reader);
            Assert.AreEqual(ordered.List.First(), meme.List.First());
        }
    }
}