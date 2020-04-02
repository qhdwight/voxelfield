using System;
using Components;
using Session.Player;

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

    public abstract class SessionStateComponentBase : ComponentBase
    {
        public StampComponent stamp;
        public Property<byte> localPlayerId;
        public ArrayProperty<PlayerStateComponent> playerStates = new ArrayProperty<PlayerStateComponent>(PlayerManager.MaxPlayers);
        public SessionSettingsComponent settings;

        public PlayerStateComponent LocalPlayerState => localPlayerId.HasValue ? playerStates[localPlayerId.Value] : null;
    }
}