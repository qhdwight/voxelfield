using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions.Components
{
    public static class StandardComponents
    {
        public static readonly IReadOnlyCollection<Type> StandardSessionElements = new List<Type>
        {
            typeof(PlayerContainerArrayProperty), typeof(LocalPlayerProperty), typeof(StampComponent), typeof(SessionSettingsComponent)
        };

        public static readonly IReadOnlyCollection<Type> StandardPlayerElements = new List<Type>
        {
            typeof(HealthProperty), typeof(MoveComponent), typeof(InventoryComponent), typeof(CameraComponent), typeof(RespawnTimerProperty),
            typeof(TeamProperty), typeof(StatsComponent), typeof(HitMarkerComponent)
        };

        public static readonly IReadOnlyCollection<Type> StandardPlayerCommandsElements = new List<Type>
        {
            typeof(InputFlagProperty), typeof(WantedItemIndexProperty), typeof(MouseComponent)
        };
    }

    [Serializable]
    public class PlayerContainerArrayProperty : ArrayProperty<Container>
    {
        public PlayerContainerArrayProperty() : base(SessionBase.MaxPlayers) { }
    }

    [Serializable]
    public class PingCheckComponent : ComponentBase
    {
        public UIntProperty tick;
    }

    [Serializable]
    public class SessionSettingsComponent : ComponentBase
    {
        public ByteProperty tickRate, modeId;

        public float TickInterval => 1.0f / tickRate;
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
}