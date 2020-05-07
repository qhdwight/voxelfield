using System.IO;
using NUnit.Framework;

namespace Swihoni.Components.Tests
{
    public class SerializerTests
    {
        [Test]
        public void TestSerializer()
        {
            OuterComponent arbitrary = OuterComponent.Arbitrary;
            var stream = new MemoryStream();
            arbitrary.Serialize(stream);

            var deserialized = new OuterComponent();
            stream.Position = 0;
            deserialized.Deserialize(stream);

            Assert.IsTrue(arbitrary.EqualTo(deserialized));
            Assert.AreNotSame(arbitrary.inner, deserialized.inner);
        }

        [Test]
        public void TestString()
        {
            var arbitrary = new StringProperty(16);
            arbitrary.SetString(builder => builder.Append("Test"));
            var stream = new MemoryStream();
            arbitrary.Serialize(stream);

            var deserialized = new StringProperty(16);
            stream.Position = 0;
            deserialized.Deserialize(stream);

            Assert.AreEqual("Test", arbitrary.GetString().ToString());
            Assert.AreEqual("Test", deserialized.GetString().ToString());
        }
    }
}