using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Sessions.Player.Visualization;

namespace Swihoni.Sessions.Player
{
    public class PlayerManager : BehaviorManagerBase
    {
        internal static readonly Pool<PlayerManager> Pool = new Pool<PlayerManager>(1, () => new PlayerManager(), UsageChanged);

        private static void UsageChanged(PlayerManager manager, bool isActive)
        {
            if (!isActive) manager.SetAllInactive();
        }
        
        public PlayerManager() : base(SessionBase.MaxPlayers, "Players") { }

        public override ArrayElementBase ExtractArray(Container session) => session.Require<PlayerContainerArrayElement>();

        public PlayerModifierDispatcherBehavior GetModifier(int playerId) => (PlayerModifierDispatcherBehavior) Modifiers[playerId];

        public PlayerVisualsDispatcherBehavior GetVisuals(int playerId) => (PlayerVisualsDispatcherBehavior) m_Visuals[playerId];
        
        
    }
}