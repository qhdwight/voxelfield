using NUnit.Framework;

namespace Components.Tests
{
    public class MergeTests
    {
        [Test]
        public void TestMerge()
        {
            var source = new OuterComponent {@double = 2.0};
            var merged = new OuterComponent {@double = 3.0, @int = 4};
            Copier.CopyTo(source, merged);
            Assert.AreEqual(merged.@double, 2.0);
            Assert.AreEqual(merged.@int, 4);
        }
    }
}