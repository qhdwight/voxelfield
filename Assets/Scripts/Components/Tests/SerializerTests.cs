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
            Assert.AreEqual(arbitrary.@int, deserialized.@int);
            Assert.AreEqual(arbitrary.@double, deserialized.@double);
            Assert.AreEqual(arbitrary.vector, deserialized.vector);
            Assert.AreNotSame(arbitrary.inner, deserialized.inner);
            Assert.AreEqual(arbitrary.inner.@uint, deserialized.inner.@uint);
            Assert.AreEqual(arbitrary.intArray, deserialized.intArray);
        }
    }
}