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

    public class AdditiveAttribute : Attribute
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
        // public UIntProperty tick;
        // public FloatProperty rtt,          // Last measured round trip time in seconds
        //                      checkElapsed, // Time elapsed since initiating check
        //                      initiateTime; // Time when check was last sent to client
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
        public UIntProperty elapsedUs;
        public BoolProperty isKill;
    }

    [Serializable, OnlyServerTrusted]
    public class DamageNotifierComponent : ComponentBase
    {
        public UIntProperty elapsedUs;
    }

    [Serializable]
    public class PlayerContainerArrayElement : ArrayElement<Container>
    {
        public PlayerContainerArrayElement() : base(SessionBase.MaxPlayers) { }
    }

    [Serializable]
    public class KillFeedComponent : ComponentBase
    {
        public ByteProperty killingPlayerId, killedPlayerId;
        public UIntProperty elapsedUs;
    }

    [Serializable]
    public class FeedComponent : ComponentBase
    {
        public StringElement feed;
    }

    [Serializable]
    public class KillFeedElement : ArrayElement<KillFeedComponent>
    {
        public KillFeedElement() : base(5) { }
    }

    // [Serializable]
    // public class PingCheckComponent : ComponentBase
    // {
    //     public UIntProperty tick;
    // }

    [Serializable]
    public class TickRateProperty : ByteProperty
    {
        public float TickInterval => 1.0f / Value;

        public uint TickIntervalUs => TimeConversions.GetUsFromSecond(TickInterval);

        public uint PlayerRenderIntervalUs => TickIntervalUs * 3;
    }

    [Serializable]
    public class ModeIdProperty : ByteProperty
    {
    }

    [Serializable]
    public class StampComponent : ComponentBase
    {
        public UIntProperty tick, timeUs, durationUs;

        public override string ToString() { return $"Tick: {tick}, Time: {timeUs}, Duration: {durationUs}"; }
    }

    [Serializable]
    public class LocalPlayerProperty : ByteProperty
    {
    }

    [Serializable]
    public class DebugClientView : Container
    {
        public DebugClientView() { }
        public DebugClientView(IEnumerable<Type> types) : base(types) { }
    }
}