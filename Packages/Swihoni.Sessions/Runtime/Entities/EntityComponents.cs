using System;
using Swihoni.Components;

namespace Swihoni.Sessions.Entities
{
    [Serializable]
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

        public IdProperty id;
    }

    [Serializable]
    public class ThrowableComponent : ComponentBase
    {
        [InterpolateRange(5.0f)] public VectorProperty position;
        public QuaternionProperty rotation;
        public ElapsedUsProperty thrownElapsedUs, contactElapsedUs, popTimeUs;
    }
}