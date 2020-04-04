using UnityEngine;

namespace Components.Tests
{
    internal class OuterComponent : ComponentBase
    {
        public FloatProperty @float;
        public InnerComponent inner;

        public UIntProperty @uint;
        public ArrayProperty<UIntProperty> intArray = new ArrayProperty<UIntProperty>(2);
        public VectorProperty vector;

        internal static OuterComponent Arbitrary => new OuterComponent
        {
            @uint = new UIntProperty(1), @float = new FloatProperty(2.0f), vector = new VectorProperty(3.0f, 4.0f, 5.0f), inner = new InnerComponent {@uint = new UIntProperty(6)},
            intArray = new ArrayProperty<UIntProperty>(new UIntProperty(7), new UIntProperty(8))
        };

        internal class InnerComponent : ComponentBase
        {
            public UIntProperty @uint;
        }
    }
}