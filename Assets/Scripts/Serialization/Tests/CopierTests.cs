using NUnit.Framework;
using UnityEngine;

namespace Serialization.Tests
{
    public class CopierTests
    {
        private class OuterClass
        {
            internal class InnerClass
            {
                public uint unsignedInteger;
            }

            public int integer;
            public double floatingPoint;
            public Vector3 vector;
            public InnerClass inner;
        }

        [Test]
        public void TestCopier()
        {
            var source = new OuterClass {integer = 2, floatingPoint = 3.7f, vector = new Vector3(3.0f, 2.0f, 1.0f), inner = new OuterClass.InnerClass {unsignedInteger = 3}};
            var destination = new OuterClass {inner = new OuterClass.InnerClass()};
            Copier.CopyTo(source, destination);
            Assert.AreEqual(source.integer, destination.integer);
            Assert.AreEqual(source.floatingPoint, destination.floatingPoint);
            Assert.AreEqual(source.vector, destination.vector);
            Assert.AreEqual(source.inner.unsignedInteger, destination.inner.unsignedInteger);
        }
    }
}