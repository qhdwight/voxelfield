using System;
using Components;
using Session.Player.Components;

namespace Session
{
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
    public abstract class SessionComponentBase : ComponentBase
    {
        public ByteProperty localPlayerId;
        public ArrayProperty<PlayerComponent> playerComponents = new ArrayProperty<PlayerComponent>(SessionBase.MaxPlayers);
        public SessionSettingsComponent settings;
        public StampComponent stamp;

        public PlayerComponent LocalPlayerComponent => localPlayerId.HasValue ? playerComponents[localPlayerId.Value] : null;
    }
}