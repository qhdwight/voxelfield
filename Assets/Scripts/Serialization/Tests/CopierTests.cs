using System.Collections.Generic;
using System.Linq;
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
                public uint @uint;
            }

            public int @int;
            public double @double;
            public Vector3 vector;
            public InnerClass inner = new InnerClass();
            public string @string;
            public List<int> listOfInts = Enumerable.Repeat(0, 2).ToList();
            public List<string> listOfStrings = Enumerable.Repeat((string) null, 1).ToList();
        }

        [Test]
        public void TestCopier()
        {
            var source = new OuterClass
            {
                @int = 2, @double = 3.7f, vector = new Vector3(3.0f, 2.0f, 1.0f), inner = new OuterClass.InnerClass {@uint = 3}, @string = "Test",
                listOfInts = new List<int> {1, 2}, listOfStrings = new List<string> {"Test"}
            };
            var destination = new OuterClass();
            Copier.CopyTo(source, destination);
            Assert.AreEqual(source.@int, destination.@int);
            Assert.AreEqual(source.@double, destination.@double);
            Assert.AreEqual(source.vector, destination.vector);
            Assert.AreEqual(source.@string, destination.@string);
            Assert.AreEqual(source.inner.@uint, destination.inner.@uint);
            Assert.AreEqual(source.listOfInts, destination.listOfInts);
            Assert.AreEqual(source.listOfStrings, destination.listOfStrings);
        }
    }
}