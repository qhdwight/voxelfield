using NUnit.Framework;

namespace Components.Tests
{
    public class CopierTests
    {
        [Test]
        public void TestEquals()
        {
            OuterComponent c1 = OuterComponent.Arbitrary, c2 = OuterComponent.Arbitrary;
            Assert.IsTrue(c1.EqualTo(c2));
        }

        [Test]
        public void TestClone()
        {
            OuterComponent component = OuterComponent.Arbitrary;
            Assert.IsTrue(component.EqualTo(component.Clone()));
        }

        [Test]
        public void TestCopier()
        {
            OuterComponent source = OuterComponent.Arbitrary;
            var destination = new OuterComponent();
            destination.MergeSet(source);
            Assert.IsTrue(destination.EqualTo(source));
        }
    }
}