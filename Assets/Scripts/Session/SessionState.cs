using System.Collections.Generic;
using Collections;
using Session.Player;

namespace Session
{
    public class SessionSettings
    {
    }

    public class SessionState
    {
        public uint tick;
        public float time, duration;
        public byte? localPlayerId;
        public readonly List<PlayerState> playerStates = ListFactory.Repeat(() => new PlayerState(), PlayerManager.MaxPlayers);
        public SessionSettings settings = new SessionSettings();

        public PlayerState LocalPlayerState => localPlayerId == null ? null : playerStates[localPlayerId.Value];
    }
}