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
            Copier.CopyTo(source, destination);
            Assert.AreEqual(source.@int, destination.@int);
            Assert.AreEqual(source.@double, destination.@double);
            Assert.AreEqual(source.vector, destination.vector);
            Assert.AreNotSame(source.inner, destination.inner);
            Assert.AreEqual(source.inner.@uint, destination.inner.@uint);
            Assert.AreEqual(source.intArray, destination.intArray);
        }
    }
}