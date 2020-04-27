using Swihoni.Components;
using Swihoni.Sessions.Player.Visualization;

namespace Swihoni.Sessions.Player
{
    public class PlayerHitboxManager : PlayerItemManagerVisualsBehavior
    {
        private PlayerHitbox[] m_PlayerHitboxes;
        private int m_PlayerId;

        public int PlayerId => m_PlayerId;

        internal override void Setup()
        {
            base.Setup();
            m_PlayerHitboxes = GetComponentsInChildren<PlayerHitbox>();
            foreach (PlayerHitbox hitbox in m_PlayerHitboxes)
            {
                hitbox.Setup(this);
            }
        }

        public override void Render(int playerId, Container playerContainer, bool isLocalPlayer)
        {
            base.Render(playerId, playerContainer, isLocalPlayer);
            m_PlayerId = playerId;
        }
    }
}