using NUnit.Framework;

namespace Swihoni.Components.Tests
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
            destination.CopyFrom(source);
            Assert.IsTrue(destination.EqualTo(source));

            var s1 = new StringElement(16);
            s1.SetString("Hi");
            var s2 = new StringElement(16);
            s2.SetString("GIAWEJGAUWEGH");
            var s3 = new StringElement(16);
            Interpolator.InterpolateInto(s1, s2, s3, 0.2f);
        }
    }
}