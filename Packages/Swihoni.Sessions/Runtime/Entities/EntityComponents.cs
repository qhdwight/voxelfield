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

    [Serializable, CustomInterpolation]
    public class ThrowableComponent : ComponentBase
    {
        [InterpolateRange(5.0f)] public VectorProperty position;
        public QuaternionProperty rotation;
        public ElapsedUsProperty thrownElapsedUs, contactElapsedUs, popTimeUs;
        public BoolProperty isFrozen;
        public ByteProperty throwerId;

        public override void CustomInterpolateFrom(ComponentBase c1, ComponentBase c2, float interpolation)
        {
            // ThrowableComponent t1 = (ThrowableComponent) c1, t2 = (ThrowableComponent) c2;
            // Note: Only first level properties as of now
            for (var i = 0; i < Count; i++)
            {
                PropertyBase p1 = (PropertyBase) c1[i], p2 = (PropertyBase) c2[i], p = (PropertyBase) this[i];
                // if (t1.thrownElapsedUs.WithoutValue || t2.thrownElapsedUs.WithoutValue || t2.thrownElapsedUs < t1.thrownElapsedUs)
                // {
                //     p.SetTo(p2);
                //     if (t1.thrownElapsedUs.WithValue && t2.thrownElapsedUs.WithValue)
                //         Debug.Log("Ok Boomer");
                // }
                // else
                p.InterpolateFrom(p1, p2, interpolation);
            }
        }
    }
}