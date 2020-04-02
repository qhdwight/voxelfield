using System;
using Collections;
using UnityEngine;
using Util;

namespace Session.Player
{
    public class PlayerManager : SingletonBehavior<PlayerManager>
    {
        public const int MaxPlayers = 2;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private PlayerVisualsBehavior[] m_Visuals;
        private PlayerModifierDispatcherBehavior[] m_Modifier;

        private static T Instantiate<T>(GameObject prefab, Action<T> setup)
        {
            var component = Instantiate(prefab).GetComponent<T>();
            setup(component);
            return component;
        }

        protected override void Awake()
        {
            base.Awake();
            m_Visuals = ArrayFactory.Repeat(() => Instantiate<PlayerVisualsBehavior>(m_PlayerVisualsPrefab, visuals => visuals.Setup()), MaxPlayers);
            m_Modifier = ArrayFactory.Repeat(() => Instantiate<PlayerModifierDispatcherBehavior>(m_PlayerModifierPrefab, modifier => modifier.Setup()), MaxPlayers);
        }

        public void Visualize(SessionStateComponentBase session)
        {
            SceneCamera.Singleton.SetEnabled(!session.localPlayerId.HasValue || session.LocalPlayerState.IsDead);
            for (var playerId = 0; playerId < session.playerStates.Length; playerId++)
                m_Visuals[playerId].Visualize(session.playerStates[playerId], playerId == session.localPlayerId);
        }

        public void ModifyCommands(byte playerId, PlayerCommands commandsToModify)
        {
            m_Modifier[playerId].ModifyCommands(commandsToModify);
        }

        public void ModifyTrusted(byte playerId, PlayerStateComponent stateToModify, PlayerCommands commands)
        {
            m_Modifier[playerId].ModifyTrusted(stateToModify, commands);
        }

        public void ModifyChecked(byte playerId, PlayerStateComponent stateToModify, PlayerCommands commands)
        {
            m_Modifier[playerId].ModifyChecked(stateToModify, commands);
        }
    }
}