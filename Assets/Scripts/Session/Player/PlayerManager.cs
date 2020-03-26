using System;
using System.Collections.Generic;
using Collections;
using Compound;
using UnityEngine;

namespace Session.Player
{
    public class PlayerManager : SingletonBehavior<PlayerManager>
    {
        public const int MaxPlayers = 2;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private List<PlayerVisualsBehavior> m_Visuals;
        private List<PlayerModifierBehavior> m_Modifier;

        private static T Instantiate<T>(GameObject prefab, Action<T> setup)
        {
            var component = Instantiate(prefab).GetComponent<T>();
            setup(component);
            return component;
        }

        protected override void Awake()
        {
            base.Awake();
            m_Visuals = ListFactory.Repeat(() => Instantiate<PlayerVisualsBehavior>(m_PlayerVisualsPrefab, visuals => visuals.Setup()), MaxPlayers);
            m_Modifier = ListFactory.Repeat(() => Instantiate<PlayerModifierBehavior>(m_PlayerModifierPrefab, modifier => modifier.Setup()), MaxPlayers);
        }

        public void Visualize(SessionState session)
        {
            SceneCamera.Singleton.SetEnabled(!session.LocalPlayerState?.isAlive ?? true);
            for (var playerId = 0; playerId < session.playerStates.Count; playerId++)
                m_Visuals[playerId].Visualize(session.playerStates[playerId], playerId == session.localPlayerId);
        }

        public void Modify(byte playerId, PlayerState state, PlayerCommands commands)
        {
            m_Modifier[playerId].Modify(state, commands);
        }
    }
}