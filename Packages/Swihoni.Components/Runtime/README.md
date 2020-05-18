## Overview

Provides a simple component package without needing to install Unity ECS and all of its 1920581295 dependencies.

Unity is the only dependency. Features include:
1) Nesting
2) Dynamic addition of elements with containers
3) Copying & Cloning
4) Serialization
5) Interpolation
6) Interfaces for creating your own procedures.

The goal is to avoid writing custom code for each data type you have in your game.

## Important concepts

1) Element. Base class that properties, arrays, and components derive from.
2) Property. Essentially an optional that wraps a value. They should always be non-null and should only have one "owner."
3) Component. Houses elements defined by fields. Supports addition of new elements, but cannot be received by type, only index.
4) Container. Component that supports retrieval of elements via their type.
5) Array element. An array of elements. For example, a string property is an array of character properties.

## Examples

```csharp
public class OuterComponent : ComponentBase
{
    public FloatProperty @float;
    public InnerComponent inner;

    public UIntProperty @uint;
    public ArrayElement<UIntProperty> intArray = new ArrayElement<UIntProperty>(2);
    public VectorProperty vector;
    public Container container = new Container(typeof(ByteProperty));

    public class InnerComponent : ComponentBase
    {
        public UIntProperty @uint;
    }
}

var source = new OuterComponent {@float = new FloatProperty(1.0f)};
var destination = new OuterComponent();
destination.CopyFrom(source);
// destination.@float is now 1.0f

var one = new OuterComponent {@float = new FloatProperty(1.0f), vector = new VectorProperty(Vector3.one)};
var two = new OuterComponent {@float = new FloatProperty(2.0f), vector = new VectorProperty(Vector3.zero)};
var interpolated = new OuterComponent();
Interpolator.InterpolateInto(one, two, interpolated, 0.5f);
// interpolated.@float.Value is now 1.5f
// interpolated.vector.Value is now {0.5f, 0.5f, 0.5f}
```

Containers provide a way to dynamically add elements and then query them by type:

```csharp
var container = new Container(); // can also use alternate constructor for list of types
container.RegisterAppend(typeof(VectorProperty));
if (container.With(out VectorProperty vector)) // true
{
    vector.Value = Vector3.forward;
}
if (container.Without<UIntProperty>()) // true
{
}
Container clone = container.Clone(); // clone will equal container and have same element types
```

My main goal of this library is for my competitive FPS game. Easily add your own attributes:

```csharp
[Serializable, ClientChecked]
public class MoveComponent : ComponentBase
{
    [Cyclic(0.0f, 1.0f)] public FloatProperty normalizedMove;
    public ByteProperty groundTick;
    [Tolerance(0.01f), InterpolateRange(2.0f)]
    public VectorProperty position, velocity;
    public FloatProperty normalizedCrouch;

    public override string ToString() => $"Position: {position}, Velocity: {velocity}";
}
```


