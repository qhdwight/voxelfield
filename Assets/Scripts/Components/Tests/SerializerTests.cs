using System.IO;
using NUnit.Framework;

namespace Components.Tests
{
    public class SerializerTests
    {
        [Test]
        public void TestSerializer()
        {
            OuterComponent arbitrary = OuterComponent.Arbitrary;
            var stream = new MemoryStream();
            Serializer.SerializeFrom(arbitrary, stream);
            var deserialized = new OuterComponent();
            Serializer.DeserializeInto(deserialized, stream);
            Assert.AreEqual(arbitrary.@uint, deserialized.@uint);
            Assert.AreEqual(arbitrary.@float, deserialized.@float, 1e-6f);
            Assert.AreEqual(arbitrary.vector, deserialized.vector);
            Assert.AreNotSame(arbitrary.inner, deserialized.inner);
            Assert.AreEqual(arbitrary.inner.@uint, deserialized.inner.@uint);
            Assert.AreEqual(arbitrary.intArray, deserialized.intArray);
        }
    }
}