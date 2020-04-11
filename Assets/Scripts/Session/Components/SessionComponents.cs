using System;
using Components;

namespace Session.Components
{
    [Serializable]
    public class PingCheckComponent : ComponentBase
    {
        public UIntProperty tick;
    }

    [Serializable]
    public class StampedPlayerComponent : ComponentBase
    {
        public ContainerBase player;
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
    public abstract class SessionContainerBase<TPlayerComponent> : ContainerBase
        where TPlayerComponent : ComponentBase
    {
        public ByteProperty localPlayerId;
        public StampComponent stamp;
        public ArrayProperty<TPlayerComponent> playerComponents;
        public SessionSettingsComponent settings;

        public ComponentBase LocalPlayerComponent => localPlayerId.HasValue ? playerComponents[localPlayerId.Value] : null;
    }
}