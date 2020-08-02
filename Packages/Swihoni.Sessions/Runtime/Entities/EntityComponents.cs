using System;
using Swihoni.Components;
using Swihoni.Sessions.Components;

namespace Swihoni.Sessions.Entities
{
    [Serializable, ModeElement]
    public class EntityArrayElement : ArrayElement<EntityContainer>
    {
        public const int Count = 10;

        public EntityArrayElement() : base(Count) { }
    }

    [Serializable]
    public class EntityContainer : Container
    {
        public EntityContainer() { }
        public EntityContainer(params Type[] types) : base(types) { }

        public ByteIdProperty id;
    }

    [Serializable]
    public class ThrowableComponent : ComponentBase
    {
        [InterpolateRange(5.0f)] public VectorProperty position;
        public QuaternionProperty rotation;
        public ElapsedUsProperty thrownElapsedUs, contactElapsedUs, popTimeUs;
    }
}