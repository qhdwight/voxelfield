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

    [Serializable, CustomInterpolation]
    public class ThrowableComponent : ComponentBase
    {
        [Flags]
        public enum Flags : byte
        {
            None = 0,
            Floating = 1,
            Persistent = 2
        }
        
        public class FlagsProperty : PropertyBase<Flags>
        {
            public override void SerializeValue(NetDataWriter writer) => writer.Put((byte) Value);
            public override void DeserializeValue(NetDataReader reader) => Value = (Flags) reader.GetByte();
            public override bool ValueEquals(in Flags value) => value == Value;
        
            public bool IsFloating
            {
                get => WithValue && (Value & Flags.Floating) == Flags.Floating;
                protected set
                {
                    SetValueIfWithout();
                    if (value) Value |= Flags.Floating;
                    else Value &= ~Flags.Floating;
                }
            }
        
            public bool IsPersistent
            {
                get => WithValue && (Value & Flags.Persistent) == Flags.Persistent;
                protected set
                {
                    SetValueIfWithout();
                    if (value) Value |= Flags.Persistent;
                    else Value &= ~Flags.Persistent;
                }
            }
        }

        [InterpolateRange(5.0f)] public VectorProperty position;
        public QuaternionProperty rotation;
        public ElapsedUsProperty thrownElapsedUs, contactElapsedUs, popTimeUs;
        public FlagsProperty flags;
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