using Components;
using Session.Player;

namespace Session
{
    public class SessionSettingsComponent : ComponentBase
    {
    }

    public abstract class SessionStateComponentBase : ComponentBase
    {
        public Property<uint> tick;
        public Property<float> time, duration;
        public Property<byte> localPlayerId;
        public ArrayProperty<PlayerStateComponent> playerStates = new ArrayProperty<PlayerStateComponent>(PlayerManager.MaxPlayers);
        public SessionSettingsComponent settings;

        public PlayerStateComponent LocalPlayerState => localPlayerId.HasValue ? playerStates[localPlayerId.Value] : null;
    }
}