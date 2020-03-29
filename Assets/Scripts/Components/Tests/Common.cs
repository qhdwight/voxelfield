using UnityEngine;

namespace Components.Tests
{
    internal class OuterComponent : ComponentBase
    {
        internal class InnerComponent : ComponentBase
        {
            public Property<uint> @uint;
        }

        public Property<int> @int;
        public OptionalProperty<double> @double;
        public Property<Vector3> vector;
        public InnerComponent inner;
        public ArrayProperty<int> intArray = new ArrayProperty<int>(2);

        internal static OuterComponent Arbitrary => new OuterComponent
        {
            @int = 2, @double = 3.7f, vector = new Vector3(3.0f, 2.0f, 1.0f), inner = new InnerComponent {@uint = 3},
            intArray = new[] {1, 2}
        };
    }
}