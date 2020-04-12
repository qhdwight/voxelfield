using NUnit.Framework;

namespace Components.Tests
{
    public class CopierTests
    {
        [Test]
        public void TestCopier()
        {
            OuterComponent source = OuterComponent.Arbitrary;
            var destination = new OuterComponent();
            destination.MergeSet(source);
            Assert.AreEqual(source.@uint, destination.@uint);
            Assert.AreEqual(source.@float, destination.@float, 1e-6f);
            Assert.AreEqual(source.vector, destination.vector);
            Assert.AreNotSame(source.inner, destination.inner);
            Assert.AreEqual(source.inner.@uint, destination.inner.@uint);
            Assert.AreEqual(source.intArray, destination.intArray);
            Assert.AreEqual(source.container.Require<ByteProperty>(), destination.container.Require<ByteProperty>());
        }
    }
}