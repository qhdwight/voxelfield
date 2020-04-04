using NUnit.Framework;
using UnityEngine;

namespace Components.Tests
{
    public class InterpolatorTests
    {
        [Test]
        public void TestInterpolation()
        {
            var one = new OuterComponent {@float = new FloatProperty(1.0f), vector = new VectorProperty(Vector3.one)};
            var two = new OuterComponent {@float = new FloatProperty(2.0f), vector = new VectorProperty(Vector3.zero)};
            var interpolated = new OuterComponent();
            Interpolator.InterpolateInto(one, two, interpolated, 0.5f);
            Assert.AreEqual(1.5f, interpolated.@float.Value);
            Assert.AreEqual(new Vector3(0.5f, 0.5f, 0.5f), interpolated.vector.Value);
        }
    }
}