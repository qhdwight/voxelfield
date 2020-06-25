using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public class PlayerManager : BehaviorManagerBase
    {
        internal static Pool<PlayerManager> pool;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            pool = new Pool<PlayerManager>(1, () => new PlayerManager(), UsageChanged);
            Application.quitting -= Cleanup;
            Application.quitting += Cleanup;
        }

        private static void Cleanup() => pool.Dispose();

        private static void UsageChanged(PlayerManager manager, bool isActive)
        {
            if (!isActive) manager.SetAllInactive();
        }

        public PlayerManager() : base(SessionBase.MaxPlayers, "Players") { }

        public override ArrayElementBase ExtractArray(Container session) => session.Require<PlayerContainerArrayElement>();

        // public PlayerModifierDispatcherBehavior GetModifier(int playerId) => (PlayerModifierDispatcherBehavior) Modifiers[playerId];

        // public PlayerVisualsDispatcherBehavior GetVisuals(int playerId) => (PlayerVisualsDispatcherBehavior) m_Visuals[playerId];
    }
}