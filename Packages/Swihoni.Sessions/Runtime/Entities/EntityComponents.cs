using System;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Sessions.Components;

namespace Swihoni.Sessions.Entities
{
    [Serializable, ModeElement]
    public class EntityArray : ArrayElement<EntityContainer>
    {
        public const int Count = 16;

        public EntityArray() : base(Count) { }
    }

    [Serializable]
    public class EntityContainer : Container
    {
        public EntityContainer() { }
        public EntityContainer(params Type[] types) : base(types) { }

        public ByteIdProperty id;
    }
    
    [Flags]
    public enum ThrowableFlags : byte
    {
        None = 0,
        Floating = 1,
        Persistent = 2
    }
    
    [Serializable]
    public class ThrowableFlagsProperty : PropertyBase<ThrowableFlags>
    {
        public override void SerializeValue(NetDataWriter writer) => writer.Put((byte) Value);
        public override void DeserializeValue(NetDataReader reader) => Value = (ThrowableFlags) reader.GetByte();
        public override bool ValueEquals(in ThrowableFlags value) => value == Value;
        
        public bool IsFloating
        {
            get => WithValue && (Value & ThrowableFlags.Floating) == ThrowableFlags.Floating;
            protected set
            {
                SetValueIfWithout();
                if (value) Value |= ThrowableFlags.Floating;
                else Value &= ~ThrowableFlags.Floating;
            }
        }
        
        public bool IsPersistent
        {
            get => WithValue && (Value & ThrowableFlags.Persistent) == ThrowableFlags.Persistent;
            protected set
            {
                SetValueIfWithout();
                if (value) Value |= ThrowableFlags.Persistent;
                else Value &= ~ThrowableFlags.Persistent;
            }
        }
    }
    

    [Serializable, CustomInterpolation]
    public class ThrowableComponent : ComponentBase
    {
        [InterpolateRange(5.0f)] public VectorProperty position;
        public QuaternionProperty rotation;
        public ElapsedUsProperty thrownElapsedUs, contactElapsedUs, popTimeUs;
        public ThrowableFlagsProperty flags;
        public ByteProperty throwerId;

        public override void CustomInterpolateFrom(ComponentBase c1, ComponentBase c2, float interpolation)
        {
            // ThrowableComponent t1 = (ThrowableComponent) c1, t2 = (ThrowableComponent) c2;
            // Note: Only first level properties as of now
            for (var i = 0; i < Count; i++)
            {
                PropertyBase p1 = (PropertyBase) c1[i], p2 = (PropertyBase) c2[i], p = (PropertyBase) this[i];
                p.InterpolateFrom(p1, p2, interpolation);
            }
        }
    }
}