using Compound;
using UnityEngine;

namespace Session.Player
{
    public class PlayerManager : SingletonBehavior<PlayerManager>, ISessionVisualizer, ISessionModifier
    {
        public const int MaxPlayers = 100;

        [SerializeField] private GameObject m_PlayerModifierPrefab, m_PlayerVisualsPrefab;

        private GameObjectMapper<byte, PlayerVisualsBehavior> m_VisualMapper;
        private GameObjectMapper<byte, PlayerModifierBehavior> m_ModifierMapper;

        protected override void Awake()
        {
            base.Awake();
            void UsageChanged(PlayerVisualsBehavior visuals, bool inUsage)
            {
                visuals.SetVisible(inUsage);
            }
            m_VisualMapper = new GameObjectMapper<byte, PlayerVisualsBehavior>(10, () => Instantiate(m_PlayerVisualsPrefab).GetComponent<PlayerVisualsBehavior>(),
                                                                               UsageChanged);
            m_ModifierMapper = new GameObjectMapper<byte, PlayerModifierBehavior>(10, () => Instantiate(m_PlayerModifierPrefab).GetComponent<PlayerModifierBehavior>(),
                                                                                  (modifier, inUsage) => { });
        }

        public void Visualize(SessionState session)
        {
            m_VisualMapper.Evaluate(session.PlayerIds, (id, visuals) => visuals.Visualize(session.playerData[id]));
        }

        public void Modify(SessionState state, SessionCommands commands)
        {
            m_ModifierMapper.Evaluate(state.PlayerIds, (id, modifier) =>
            {
                modifier.Modify(state.playerData[id], commands.playerCommands[id]);
            });
        }
    }
}