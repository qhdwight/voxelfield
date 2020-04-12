using System;
using System.Collections.Generic;
using Components;
using Session.Player.Components;

namespace Session.Components
{
    public static class StandardComponents
    {
        public static readonly List<Type> StandardSessionComponents = new List<Type>
        {
            typeof(PlayerContainerArrayProperty), typeof(LocalPlayerProperty), typeof(StampComponent), typeof(SessionSettingsComponent)
        };

        public static readonly List<Type> StandardPlayerComponents = new List<Type>
        {
            typeof(HealthProperty), typeof(MoveComponent), typeof(InventoryComponent), typeof(CameraComponent)
        };

        public static readonly List<Type> StandardPlayerCommandsComponents = new List<Type>
        {
            typeof(InputFlagProperty), typeof(WantedItemIndexProperty), typeof(MouseComponent)
        };
    }

    [Serializable]
    public class PlayerContainerArrayProperty : ArrayProperty<Container>
    {
        public PlayerContainerArrayProperty() : base(SessionBase.MaxPlayers)
        {
        }
    }

    [Serializable]
    public class PingCheckComponent : ComponentBase
    {
        public UIntProperty tick;
    }

    [Serializable]
    public class StampedPlayerComponent : ComponentBase
    {
        public Container player;
        public StampComponent stamp;
    }

    [Serializable]
    public class SessionSettingsComponent : ComponentBase
    {
        public ByteProperty tickRate;
    }

    [Serializable]
    public class StampComponent : ComponentBase
    {
        public UIntProperty tick;
        public FloatProperty time, duration;
    }

    [Serializable]
    public class LocalPlayerProperty : ByteProperty
    {
    }
}