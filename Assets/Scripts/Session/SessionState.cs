using System.Collections.Generic;
using System.Linq;
using Session.Player;

namespace Session
{
    public class GameSettings
    {
        public string mapName;
    }

    public class SessionState
    {
        public uint tick;
        public byte? localPlayerId;
        public List<PlayerData> playerData = Enumerable.Range(1, PlayerManager.MaxPlayers).Select(i => i == 1 ? new PlayerData() : null).ToList();
        public GameSettings settings;

        public PlayerData LocalPlayerData => localPlayerId == null ? null : playerData[localPlayerId.Value];

        public HashSet<byte> PlayerIds => new HashSet<byte>(playerData.Where(player => player != null).Select((player, id) => (byte) id));
    }
}