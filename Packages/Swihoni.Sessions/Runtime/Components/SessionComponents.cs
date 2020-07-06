using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Util;

namespace Swihoni.Sessions.Components
{
    using ServerOnly = NoSerialization;

    public class OnlyServerTrustedAttribute : Attribute
    {
    }

    public class ClientTrustedAttribute : Attribute
    {
    }

    public class ClientCheckedAttribute : Attribute
    {
    }

    /* Server */

    [Serializable]
    public class ServerSessionContainer : Container
    {
        public ServerSessionContainer() { }
        public ServerSessionContainer(IEnumerable<Type> types) : base(types) { }
    }

    [Serializable]
    public class ServerStampComponent : StampComponent
    {
    }

    [Serializable, ServerOnly]
    public class ServerTag : ComponentBase
    {
    }

    [Serializable, ServerOnly]
    public class ServerPingComponent : ComponentBase
    {
        public UIntProperty latencyUs;
    }
    
    [Serializable, ServerOnly]
    public class HasSentInitialData : BoolProperty
    {
    }

    /* Client */

    [Serializable]
    public class ClientCommandsContainer : Container
    {
        public ClientCommandsContainer() { }
        public ClientCommandsContainer(IEnumerable<Type> types) : base(types) { }
    }

    [Serializable, ClientTrusted]
    public class ClientStampComponent : StampComponent
    {
    }

    [Serializable, ClientTrusted]
    public class AcknowledgedServerTickProperty : UIntProperty
    {
    }

    /// <summary>
    /// Use to put server time of updates into client time.
    /// </summary>
    [Serializable]
    public class LocalizedClientStampComponent : StampComponent
    {
    }

    /* Shared */

    [Serializable, OnlyServerTrusted]
    public class HitMarkerComponent : ComponentBase
    {
        public UIntTimeProperty elapsedUs;
        public BoolProperty isKill;
    }

    [Serializable, OnlyServerTrusted]
    public class DamageNotifierComponent : ComponentBase
    {
        public UIntTimeProperty elapsedUs;
        public ByteProperty damage, inflictingPlayerId;
    }

    [Serializable]
    public class PlayerContainerArrayElement : ArrayElement<Container>
    {
        public PlayerContainerArrayElement() : base(SessionBase.MaxPlayers) { }
    }

    [Serializable]
    public class KillFeedComponent : ComponentBase
    {
        public UIntTimeProperty elapsedUs;
        public ByteProperty killingPlayerId, killedPlayerId;
        public StringProperty weaponName = new StringProperty(32);
    }

    [Serializable]
    public class KillFeedElement : ArrayElement<KillFeedComponent>
    {
        public KillFeedElement() : base(5) { }
    }

    [Serializable]
    public class TickRateProperty : ByteProperty
    {
        public float TickInterval => 1.0f / Value;

        public uint TickIntervalUs => TimeConversions.GetUsFromSecond(TickInterval);

        public uint PlayerRenderIntervalUs => TickIntervalUs * 3;

        public override string ToString() => $"Seconds: {TickInterval}, Microseconds: {TickIntervalUs}";
    }

    [Serializable]
    public class ModeIdProperty : ByteProperty
    {
        public const byte Deathmatch = 0, Showdown = 1, Ctf = 2, Designer = 3;

        public ModeIdProperty(byte value) : base(value) { }
        public ModeIdProperty() { }
    }

    [Serializable]
    public class StampComponent : ComponentBase
    {
        public UIntProperty tick, timeUs, durationUs;

        public override string ToString() => $"Tick: {tick}, Time: {timeUs}, Duration: {durationUs}";
    }

    [Serializable, ClientTrusted]
    public class UsernameProperty : StringProperty
    {
        public UsernameProperty() : base(32) { }
    }

    [Serializable]
    public class LocalPlayerId : ByteProperty
    {
    }

    [Serializable]
    public class DebugClientView : Container
    {
        public DebugClientView() { }
        public DebugClientView(IEnumerable<Type> types) : base(types) { }
    }

    [Serializable, ClientTrusted, SingleTick]
    public class StringCommandProperty : StringProperty
    {
        public StringCommandProperty() { }
        public StringCommandProperty(string @string) : base(@string) { }
    }
}