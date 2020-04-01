using UnityEngine;

namespace Components.Tests
{
    using IntArrayProperty = ArrayProperty<Property<int>>;

    internal class OuterComponent : ComponentBase
    {
        public Property<float> @float;
        public InnerComponent inner;

        public Property<int> @int;
        public IntArrayProperty intArray = new IntArrayProperty(2);
        public Property<Vector3> vector;

        internal static OuterComponent Arbitrary => new OuterComponent
        {
            @int = 2, @float = 3.7f, vector = new Vector3(3.0f, 2.0f, 1.0f), inner = new InnerComponent {@uint = 3},
            intArray = IntArrayProperty.From(1, 2)
        };

        internal class InnerComponent : ComponentBase
        {
            public Property<uint> @uint;
        }
    }
}