using System;

namespace Swihoni.Components.Tests
{
    [Serializable]
    internal class OuterComponent : ComponentBase
    {
        public FloatProperty @float;
        public InnerComponent inner;

        public UIntProperty @uint;
        public ArrayElement<UIntProperty> intArray = new ArrayElement<UIntProperty>(2);
        public VectorProperty vector;
        public Container container = new Container(typeof(ByteProperty));

        internal static OuterComponent Arbitrary
        {
            get
            {
                var component = new OuterComponent
                {
                    @uint = new UIntProperty(1), @float = new FloatProperty(2.0f), vector = new VectorProperty(3.0f, 4.0f, 5.0f),
                    inner = new InnerComponent {@uint = new UIntProperty(6)},
                    intArray = new ArrayElement<UIntProperty>(new UIntProperty(7), new UIntProperty(8))
                };
                component.container.Require<ByteProperty>().Value = 5;
                return component;
            }
        }

        internal class InnerComponent : ComponentBase
        {
            public UIntProperty @uint;
        }
    }
}