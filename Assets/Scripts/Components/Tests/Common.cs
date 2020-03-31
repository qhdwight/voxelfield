using UnityEngine;

namespace Components.Tests
{
    using IntArrayProperty = ArrayProperty<Property<int>>;

    internal class OuterComponent : ComponentBase
    {
        internal class InnerComponent : ComponentBase
        {
            public Property<uint> @uint;
        }

        public Property<int> @int;
        public Property<double> @double;
        public Property<Vector3> vector;
        public InnerComponent inner;
        public IntArrayProperty intArray = new IntArrayProperty(2);

        internal static OuterComponent Arbitrary => new OuterComponent
        {
            @int = 2, @double = 3.7f, vector = new Vector3(3.0f, 2.0f, 1.0f), inner = new InnerComponent {@uint = 3},
            intArray = IntArrayProperty.From(1, 2)
        };
    }
}