using NUnit.Framework;
using UnityEngine;

namespace Components.Tests
{
    public class InterpolatorTests
    {
        [Test]
        public void TestInterpolation()
        {
            var source = new OuterComponent {@float = 1.0f, vector = Vector3.one};
            var destination = new OuterComponent {@float = 2.0f, vector = Vector3.zero};
            Interpolator.InterpolateInto(source, destination, 0.5f);
            Assert.AreEqual(1.5f, destination.@float.Value);
            Assert.AreEqual(new Vector3(0.5f, 0.5f, 0.5f), destination.vector.Value);
        }
    }
}