using System.IO;
using NUnit.Framework;

namespace Serialization.Tests
{
    public class SerializerTests
    {
        [Test]
        public void TestSerializer()
        {
            OuterClass arbitrary = OuterClass.Arbitrary;
            var stream = new MemoryStream();
            Serializer.Serialize(arbitrary, stream);
            var deserialized = new OuterClass();
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