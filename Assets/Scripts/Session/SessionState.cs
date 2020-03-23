using System.Collections.Generic;
using UnityEngine;

namespace Session
{
    public class PlayerData
    {
        public int id;
        public Vector3 position;
    }

    public class GameSettings
    {
        public string mapName;
    }

    public class SessionState
    {
        public uint tick;
        public byte? localPlayerId;
        public List<PlayerData> playerData;
        public GameSettings settings;

        public PlayerData LocalPlayerData => localPlayerId == null ? null : playerData[localPlayerId.Value];
    }
}