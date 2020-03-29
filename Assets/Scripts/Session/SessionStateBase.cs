using Collections;
using Session.Player;

namespace Session
{
    public class SessionSettings
    {
    }

    public abstract class SessionStateBase
    {
        public uint tick;
        public float time, duration;
        public byte? localPlayerId;
        public readonly PlayerState[] playerStates = ArrayFactory.Repeat(() => new PlayerState(), PlayerManager.MaxPlayers);
        public SessionSettings settings = new SessionSettings();

        public PlayerState LocalPlayerState => localPlayerId.HasValue ? playerStates[localPlayerId.Value] : null;

        public bool TryGetLocalState(out PlayerState localState)
        {
            localState = LocalPlayerState;
            return localState != null;
        }
    }
}