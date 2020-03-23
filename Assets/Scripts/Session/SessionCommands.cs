using System.Collections.Generic;
using System.Linq;
using Session.Player;

namespace Session
{
    public class SessionCommands
    {
        public List<PlayerCommands> playerCommands = Enumerable.Range(1, PlayerManager.MaxPlayers).Select(i => i == 1 ? new PlayerCommands() : null).ToList();
    }
}