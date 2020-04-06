using Components;
using NUnit.Framework;
using Session.Player.Components;

namespace Session.Tests
{
    public class InterpolationTest
    {
        [Test]
        public void TestInterpolation()
        {
            var i1 = new ItemComponent {id = new ByteProperty(1), status = new ByteStatusComponent {id = new ByteProperty(0), elapsed = new FloatProperty(1.870f)}};
            var i2 = new ItemComponent {id = new ByteProperty(1), status = new ByteStatusComponent {id = new ByteProperty(1), elapsed = new FloatProperty(0.038f)}};
            var id = new ItemComponent();
            Interpolator.InterpolateInto(i1, i2, id, 0.9f);
            Assert.AreEqual(1, id.status.id.Value);
            Assert.AreEqual(0.021f, id.status.elapsed.Value, 1e-3f);
        }
    }
}