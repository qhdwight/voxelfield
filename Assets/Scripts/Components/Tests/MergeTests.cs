using NUnit.Framework;

namespace Components.Tests
{
    public class MergeTests
    {
        [Test]
        public void TestMerge()
        {
            var source = new OuterComponent {@float = 2.0f};
            var merged = new OuterComponent {@float = 3.0f, @int = 4};
            Copier.CopyTo(source, merged);
            Assert.AreEqual(merged.@float, 2.0f);
            Assert.AreEqual(merged.@int, 4);
        }
    }
}