using LiteNetLib.Utils;
using NUnit.Framework;

namespace Swihoni.Components.Tests
{
    public class SerializerTests
    {
        [Test]
        public void TestSerializer()
        {
            OuterComponent arbitrary = OuterComponent.Arbitrary;
            var writer = new NetDataWriter();
            arbitrary.Serialize(writer);

            var deserialized = new OuterComponent();
            var reader = new NetDataReader(writer.Data);
            deserialized.Deserialize(reader);

            Assert.IsTrue(arbitrary.EqualTo(deserialized));
            Assert.AreNotSame(arbitrary.inner, deserialized.inner);
        }

        [Test]
        public void TestString()
        {
            var arbitrary = new StringProperty("Test");
            var writer = new NetDataWriter();
            arbitrary.Serialize(writer);

            var deserialized = new StringProperty(4);
            var reader = new NetDataReader(writer.Data);
            deserialized.Deserialize(reader);

            Assert.AreEqual("Test", arbitrary.Builder.ToString());
            Assert.AreEqual("Test", deserialized.Builder.ToString());
        }
    }
}