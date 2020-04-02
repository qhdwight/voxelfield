using System;
using Components;
using Session.Player;
using Session.Player.Components;

namespace Session
{
    [Serializable]
    public class SessionSettingsComponent : ComponentBase
    {
        public Property<byte> tickRate;
    }

    public class StampComponent : ComponentBase
    {
        public Property<uint> tick;
        public Property<float> time, duration;
    }

    public abstract class SessionComponentBase : ComponentBase
    {
        public Property<byte> localPlayerId;
        public ArrayProperty<PlayerComponent> playerComponents = new ArrayProperty<PlayerComponent>(PlayerManager.MaxPlayers);
        public SessionSettingsComponent settings;
        public StampComponent stamp;

        public PlayerComponent LocalPlayerComponent => localPlayerId.HasValue ? playerComponents[localPlayerId.Value] : null;
    }
}