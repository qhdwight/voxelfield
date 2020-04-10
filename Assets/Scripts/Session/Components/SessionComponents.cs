using System;
using Components;
using Session.Player.Components;

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
    public abstract class SessionContainerBase : ContainerBase
    {
        public ByteProperty localPlayerId;
        public StampComponent stamp;

        public abstract Type PlayerType { get; }

        public abstract ArrayProperty<ContainerBase> PlayerComponents { get; }
    }

    [Serializable]
    public abstract class SessionContainerBase<TPlayerComponent> : SessionContainerBase
        where TPlayerComponent : ContainerBase
    {
        public ArrayProperty<TPlayerComponent> playerComponents = new ArrayProperty<TPlayerComponent>(SessionBase.MaxPlayers);
        public SessionSettingsComponent settings;

        public ComponentBase LocalPlayerComponent => localPlayerId.HasValue ? playerComponents[localPlayerId.Value] : null;

        public override Type PlayerType => typeof(TPlayerComponent);

        public override ArrayProperty<ContainerBase> PlayerComponents => (object) playerComponents as ArrayProperty<ContainerBase>;
    }
}