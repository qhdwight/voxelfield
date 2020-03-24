using Compound;
using UnityEngine;

namespace Session.Player
{
    public class PlayerManager : SingletonBehavior<PlayerManager>
    {
        public const int MaxPlayers = 100;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private Mapper<byte, PlayerVisualsBehavior> m_VisualMapper;
        private Mapper<byte, PlayerModifierBehavior> m_ModifierMapper;

        protected override void Awake()
        {
            base.Awake();
            void UsageChanged(PlayerVisualsBehavior visuals, bool inUsage)
            {
                visuals.SetVisible(inUsage);
            }
            m_VisualMapper = new Mapper<byte, PlayerVisualsBehavior>(10, () => Instantiate(m_PlayerVisualsPrefab).GetComponent<PlayerVisualsBehavior>(),
                                                                     UsageChanged);
            m_ModifierMapper = new Mapper<byte, PlayerModifierBehavior>(10, () => Instantiate(m_PlayerModifierPrefab).GetComponent<PlayerModifierBehavior>(),
                                                                        (modifier, inUsage) => { });
        }

        public void Visualize(SessionState session)
        {
            m_VisualMapper.Synchronize(session.PlayerIds, (id, visuals) => visuals.Visualize(session.playerData[id]));
        }

        public void Modify(SessionState state, byte playerId, PlayerCommands commands)
        {
            m_ModifierMapper.Synchronize(state.PlayerIds);
            m_ModifierMapper.Execute(playerId, modifier => modifier.Modify(state.playerData[playerId], commands));
        }
    }
}