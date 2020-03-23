using UnityEngine;

namespace Session.Player
{
    public class PlayerManager : SingletonBehavior<PlayerManager>, ISessionVisualizer, ISessionModifier
    {
        [SerializeField] private GameObject m_PlayerPrefab, m_PlayerVisualsPrefab;

        public void Visualize(SessionState session)
        {
        }

        public void Modify(SessionState session)
        {
            PlayerData localPlayerData = session.LocalPlayerData;
            if (localPlayerData != null)
            {
                PlayerMovement.Move(localPlayerData);
            }
        }
    }
}