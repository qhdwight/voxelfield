using System;
using System.Collections.Generic;
using Swihoni.Components;

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
        public UIntProperty tick;
        public FloatProperty rtt,          // Last measured round trip time in seconds
                             checkElapsed, // Time elapsed since initiating check
                             initiateTime; // Time when check was last sent to client
    }

    /* Client */

    [Serializable]
    public class ClientCommandsContainer : Container
    {
        public ClientCommandsContainer() { }
        public ClientCommandsContainer(IEnumerable<Type> types) : base(types) { }
    }

    [Serializable]
    public class ClientStampComponent : StampComponent
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
        public FloatProperty elapsed;
        public BoolProperty isKill;
    }

    [Serializable, OnlyServerTrusted]
    public class DamageNotifierComponent : ComponentBase
    {
        public FloatProperty elapsed;
    }

    [Serializable]
    public class PlayerContainerArrayProperty : ArrayProperty<Container>
    {
        public PlayerContainerArrayProperty() : base(SessionBase.MaxPlayers) { }
    }

    [Serializable]
    public class KillFeedComponent : ComponentBase
    {
        public ByteProperty killingPlayerId, killedPlayerId;
        public FloatProperty elapsed;
    }

    [Serializable]
    public class FeedComponent : ComponentBase
    {
        public StringProperty feed;
    }

    [Serializable]
    public class KillFeedProperty : ArrayProperty<KillFeedComponent>
    {
        public KillFeedProperty() : base(5) { }
    }

    [Serializable]
    public class PingCheckComponent : ComponentBase
    {
        public UIntProperty tick;
    }

    [Serializable]
    public class TickRateProperty : ByteProperty
    {
        public float TickInterval => 1.0f / Value;
    }

    [Serializable]
    public class ModeIdProperty : ByteProperty
    {
    }

    [Serializable]
    public class StampComponent : ComponentBase
    {
        public UIntProperty tick;
        public FloatProperty time, duration;

        public override string ToString() { return $"Tick: {tick}, Time: {time}, Duration: {duration}"; }
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