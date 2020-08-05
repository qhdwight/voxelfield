using Swihoni.Components;

namespace Swihoni.Sessions.Player
{
    public class PlayerHitboxManager : PlayerVisualizerBehaviorBase
    {
        private PlayerHitbox[] m_PlayerHitboxes;
        public Container CurrentPlayer { get; private set; }

        internal override void Setup()
        {
            base.Setup();
            m_PlayerHitboxes = GetComponentsInChildren<PlayerHitbox>();
            foreach (PlayerHitbox hitbox in m_PlayerHitboxes)
                hitbox.Setup(this);
        }

        public override void Evaluate(in SessionContext context)
        {
            CurrentPlayer = context.player;
            base.Evaluate(context);
        }
    }
}