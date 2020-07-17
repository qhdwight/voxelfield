using System;
using System.Collections.Generic;
using Console;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerModifierDispatcherBehavior : ModifierBehaviorBase, IDisposable
    {
        private PlayerModifierBehaviorBase[] m_Modifiers;
        private PlayerHitboxManager m_HitboxManager;
        private PlayerTrigger m_Trigger;
        private SessionBase m_Session;

        private PlayerMovement m_Movement;
        public PlayerMovement Movement
        {
            get
            {
                if (m_Movement == null)
                    m_Movement = GetComponentInChildren<PlayerMovement>();
                return m_Movement;
            }
        }

        internal void Setup(SessionBase session, int playerId)
        {
            m_Session = session;
            m_Modifiers = GetComponents<PlayerModifierBehaviorBase>();
            m_HitboxManager = GetComponent<PlayerHitboxManager>();
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers)
                modifier.Setup(session);
            if (m_HitboxManager) m_HitboxManager.Setup(session);
            m_Trigger = GetComponentInChildren<PlayerTrigger>(true);
            if (m_Trigger) m_Trigger.Setup(playerId);
        }

        public void ModifyChecked(SessionBase session, int playerId, Container playerToModify, Container commands, uint durationUs, int tickDelta = 1)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(session, playerId, playerToModify, commands, durationUs, tickDelta);

            if (PlayerModifierBehaviorBase.TryServerCommands(playerToModify, out IEnumerable<string[]> stringCommands))
                foreach (string[] stringCommand in stringCommands)
                    ConfigManagerBase.HandleArgs(stringCommand);
        }

        public void ModifyTrusted(SessionBase session, int playerId, Container trustedPlayer, Container verifiedPlayer, Container commands, uint durationUs)
        {
            // if (UnityEngine.Input.GetKeyDown(KeyCode.T)) trustedPlayer.Require<IdProperty>().Value = 0;
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyTrusted(session, playerId, trustedPlayer, verifiedPlayer, commands, durationUs);
        }

        public void Synchronize(Container player)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.SynchronizeBehavior(player);
        }

        public void ModifyCommands(SessionBase session, Container commandsToModify, int playerId)
        {
            if (SessionBase.InterruptingInterface)
            {
                // TODO:refactor attribute
                commandsToModify.Require<MouseComponent>().Zero();
                commandsToModify.Require<InputFlagProperty>().Zero();
                return;
            }
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyCommands(session, commandsToModify, playerId);
        }

        public void EvaluateHitboxes(SessionBase session, int playerId, Container player) => m_HitboxManager.Evaluate(session, playerId, player);

        public void Dispose()
        {
            if (m_HitboxManager) m_HitboxManager.Dispose();
        }
    }

    public abstract class PlayerModifierBehaviorBase : MonoBehaviour
    {
        protected SessionBase m_Session;

        internal virtual void Setup(SessionBase session) => m_Session = session;

        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        public virtual void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, uint durationUs, int tickDelta) => SynchronizeBehavior(player);

        public static bool TryServerCommands(Container player, out IEnumerable<string[]> commands)
        {
            if (player.Without<ServerTag>() || player.WithoutPropertyOrWithoutValue(out StringCommandProperty command) || command.Builder.Length == 0)
            {
                commands = default;
                return false;
            }
            commands = ConsoleCommandExecutor.GetArgs(command.Builder.ToString());
            return true;
        }
        
        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        public virtual void ModifyTrusted(SessionBase session, int playerId, Container trustedPlayer, Container player, Container commands, uint durationUs) =>
            SynchronizeBehavior(trustedPlayer);

        public virtual void ModifyCommands(SessionBase session, Container commands, int playerId) { }

        protected internal virtual void SynchronizeBehavior(Container player) { }
    }
}