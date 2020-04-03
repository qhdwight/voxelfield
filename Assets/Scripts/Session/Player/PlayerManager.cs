using System;
using Collections;
using Session.Player.Components;
using Session.Player.Modifiers;
using Session.Player.Visualization;
using UnityEngine;
using Util;

namespace Session.Player
{
    public class PlayerManager : SingletonBehavior<PlayerManager>
    {
        public const int MaxPlayers = 2;
        private PlayerModifierDispatcherBehavior[] m_Modifier;

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private PlayerVisualsDispatcherBehavior[] m_Visuals;

        private static T Instantiate<T>(GameObject prefab, Action<T> setup)
        {
            var component = Instantiate(prefab).GetComponent<T>();
            setup(component);
            return component;
        }

        protected override void Awake()
        {
            base.Awake();
            m_Visuals = ArrayFactory.Repeat(() => Instantiate<PlayerVisualsDispatcherBehavior>(m_PlayerVisualsPrefab, visuals => visuals.Setup()), MaxPlayers);
            m_Modifier = ArrayFactory.Repeat(() => Instantiate<PlayerModifierDispatcherBehavior>(m_PlayerModifierPrefab, modifier => modifier.Setup()), MaxPlayers);
        }

        public void Visualize(SessionComponentBase session)
        {
            SceneCamera.Singleton.SetEnabled(!session.localPlayerId.HasValue || session.LocalPlayerComponent.IsDead);
            for (var playerId = 0; playerId < session.playerComponents.Length; playerId++)
                m_Visuals[playerId].Visualize(session.playerComponents[playerId], playerId == session.localPlayerId);
        }

        public void ModifyCommands(byte playerId, PlayerCommandsComponent commandsToModify)
        {
            m_Modifier[playerId].ModifyCommands(commandsToModify);
        }

        public void ModifyTrusted(byte playerId, PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            m_Modifier[playerId].ModifyTrusted(componentToModify, commands);
        }

        public void ModifyChecked(byte playerId, PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            m_Modifier[playerId].ModifyChecked(componentToModify, commands);
        }
    }
}