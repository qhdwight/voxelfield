namespace Swihoni.Sessions.Player
{
    public class PlayerHitboxManager : PlayerVisualizerBase
    {
        private PlayerHitbox[] m_PlayerHitboxes;

        internal override void Setup(SessionBase session)
        {
            base.Setup(session);
            m_PlayerHitboxes = GetComponentsInChildren<PlayerHitbox>();
            foreach (PlayerHitbox hitbox in m_PlayerHitboxes)
                hitbox.Setup(this);
        }
    }
}