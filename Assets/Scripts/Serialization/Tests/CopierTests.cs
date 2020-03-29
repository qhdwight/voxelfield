using NUnit.Framework;

namespace Serialization.Tests
{
    public class CopierTests
    {
        [Test]
        public void TestCopier()
        {
            OuterClass source = OuterClass.Arbitrary;
            var destination = new OuterClass();
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