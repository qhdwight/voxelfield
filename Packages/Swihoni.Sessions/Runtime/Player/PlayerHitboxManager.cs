using Swihoni.Components;
using Swihoni.Sessions.Player.Visualization;

namespace Swihoni.Sessions.Player
{
    public class PlayerHitboxManager : PlayerItemManagerVisualsBehavior
    {
        private PlayerHitbox[] m_PlayerHitboxes;
        private PlayerBodyAnimatorBehavior m_BodyAnimator;

        public int PlayerId { get; private set; }

        internal override void Setup()
        {
            base.Setup();
            m_PlayerHitboxes = GetComponentsInChildren<PlayerHitbox>();
            m_BodyAnimator = GetComponent<PlayerBodyAnimatorBehavior>();
            foreach (PlayerHitbox hitbox in m_PlayerHitboxes)
                hitbox.Setup(this);
            m_BodyAnimator.Setup();
        }

        public void Evaluate(int playerId, Container player)
        {
            Render(playerId, player, false);
        }
        
        public override void Render(int playerId, Container player, bool isLocalPlayer)
        {
            base.Render(playerId, player, isLocalPlayer);
            if (m_BodyAnimator) m_BodyAnimator.Render(playerId, player, isLocalPlayer);
            PlayerId = playerId;
        }
    }
}