using Swihoni.Components;

namespace Swihoni.Sessions.Player
{
    public class PlayerHitboxManager : PlayerVisualizerBase
    {
        private PlayerHitbox[] m_PlayerHitboxes;
        public Container CurrentPlayer { get; private set; }

        internal override void Setup(SessionBase session)
        {
            base.Setup(session);
            m_PlayerHitboxes = GetComponentsInChildren<PlayerHitbox>();
            foreach (PlayerHitbox hitbox in m_PlayerHitboxes)
                hitbox.Setup(this);
        }

        public override void Evaluate(SessionBase session, int playerId, Container player)
        {
            CurrentPlayer = player;
            base.Evaluate(session, playerId, player);
        }
    }
}