using UnityEngine;

namespace Serialization.Tests
{
    internal class OuterClass
    {
        internal class InnerClass
        {
            public uint @uint;
        }

        public int @int;
        public double @double;
        public Vector3 vector;
        public InnerClass inner = new InnerClass();
        public int[] intArray = new int[2];

        internal static OuterClass Arbitrary => new OuterClass
        {
            @int = 2, @double = 3.7f, vector = new Vector3(3.0f, 2.0f, 1.0f), inner = new InnerClass {@uint = 3},
            intArray = new[] {1, 2}
        };
    }
}