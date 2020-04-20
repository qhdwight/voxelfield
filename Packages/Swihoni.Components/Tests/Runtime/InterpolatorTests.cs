using NUnit.Framework;
using UnityEngine;

namespace Swihoni.Components.Tests
{
    public class InterpolatorTests
    {
        private class CyclicComponent : ComponentBase
        {
            [Cyclic(0.0f, 1.0f)] public FloatProperty @float;
        }

        [Test]
        public void TestInterpolation()
        {
            {
                var one = new OuterComponent {@float = new FloatProperty(1.0f), vector = new VectorProperty(Vector3.one)};
                var two = new OuterComponent {@float = new FloatProperty(2.0f), vector = new VectorProperty(Vector3.zero)};
                var interpolated = new OuterComponent();
                Interpolator.InterpolateInto(one, two, interpolated, 0.5f);
                Assert.AreEqual(1.5f, interpolated.@float.Value, 1e-6f);
                Assert.AreEqual(new Vector3(0.5f, 0.5f, 0.5f), interpolated.vector.Value);
            }
            {
                var one = new CyclicComponent {@float = new FloatProperty(0.9f)};
                var two = new CyclicComponent {@float = new FloatProperty(0.3f)};
                var interpolated = new CyclicComponent();
                Interpolator.InterpolateInto(one, two, interpolated, 0.5f);
                Assert.AreEqual(0.1f, interpolated.@float.Value, 1e-6f);
            }
        }
    }
}