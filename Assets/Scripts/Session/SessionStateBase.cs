using Components;
using Session.Player;

namespace Session
{
    public class SessionSettings : ComponentBase
    {
    }

    public abstract class SessionStateBase : ComponentBase
    {
        public Property<uint> tick;
        public Property<float> time, duration;
        public OptionalProperty<byte> localPlayerId;
        public readonly ArrayProperty<PlayerState> playerStates = new ArrayProperty<PlayerState>(PlayerManager.MaxPlayers);
        public SessionSettings settings;

        public PlayerState LocalPlayerState => localPlayerId.HasValue ? playerStates[localPlayerId.Value] : null;
    }
}