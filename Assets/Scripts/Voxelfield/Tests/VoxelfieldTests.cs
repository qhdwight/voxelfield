using LiteNetLib.Utils;
using NUnit.Framework;
using Voxel;

namespace Voxelfield.Tests
{
    public static class VoxelfieldTests
    {
        [Test]
        public static void TestVoxelSerialization()
        {
            var change = new VoxelChange{magnitude = 5};
            var writer = new NetDataWriter();
            VoxelVersionSerializer.Serialize(change, writer);
            var reader = new NetDataReader(writer.Data);

            VoxelChange deserialized = VoxelVersionSerializer.Deserialize(reader);
            Assert.AreEqual(change.magnitude, deserialized.magnitude);
        }
    }
}