using NUnit.Framework;

namespace Components.Tests
{
    public class CopierTests
    {
        [Test]
        public void TestEquals()
        {
            OuterComponent c1 = OuterComponent.Arbitrary, c2 = OuterComponent.Arbitrary;
            Assert.IsTrue(c1.AreEquals(c2));
        }

        [Test]
        public void TestClone()
        {
            OuterComponent component = OuterComponent.Arbitrary;
            Assert.IsTrue(component.AreEquals(component.Clone()));
        }

        [Test]
        public void TestCopier()
        {
            OuterComponent source = OuterComponent.Arbitrary;
            var destination = new OuterComponent();
            destination.MergeSet(source);
            Assert.IsTrue(destination.AreEquals(source));
        }
    }
}