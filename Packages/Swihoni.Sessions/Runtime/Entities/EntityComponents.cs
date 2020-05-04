using System;
using Swihoni.Components;

namespace Swihoni.Sessions.Entities
{
    [Serializable]
    public class EntityContainer : Container
    {
        public EntityContainer() : base(typeof(EntityId)) {
        }
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