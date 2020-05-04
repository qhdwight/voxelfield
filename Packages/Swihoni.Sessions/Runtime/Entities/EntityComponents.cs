using System;
using Swihoni.Components;

namespace Swihoni.Sessions.Entities
{
    [Serializable]
    public class EntityArrayProperty : ArrayProperty<EntityContainer>
    {
        public EntityArrayProperty() : base(10) { }
    }

    [Serializable]
    public class EntityContainer : Container
    {
        public EntityContainer() { }
        public EntityContainer(params Type[] types) : base(types) { }

        public EntityId id;
    }

    [Serializable]
    public class EntityId : ByteProperty
    {
        public const byte None = 0, Grenade = None + 1;
    }

    [Serializable]
    public class ThrowableComponent : ComponentBase
    {
        public VectorProperty position;
        public QuaternionProperty rotation;
    }
}