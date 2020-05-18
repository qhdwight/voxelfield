using NUnit.Framework;

namespace Swihoni.Components.Tests
{
    public class MergeTests
    {
        [Test]
        public void TestMerge()
        {
            var source = new OuterComponent {@float = new FloatProperty(1.0f)};
            var merged = new OuterComponent {@float = new FloatProperty(2.0f), @uint = new UIntProperty(3)};
            merged.MergeFrom(source);
            Assert.AreEqual(1.0f, merged.@float.Value, 1e-6f);
            Assert.AreEqual(3, merged.@uint.Value);
        }
    }
}