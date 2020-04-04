using NUnit.Framework;

namespace Components.Tests
{
    public class MergeTests
    {
        [Test]
        public void TestMerge()
        {
            var source = new OuterComponent {@float = new FloatProperty(1.0f)};
            var merged = new OuterComponent {@float = new FloatProperty(2.0f), @uint = new UIntProperty(3)};
            Copier.CopyTo(source, merged);
            Assert.AreEqual(1.0f, merged.@float.Value);
            Assert.AreEqual(3, merged.@uint.Value);
        }
    }
}