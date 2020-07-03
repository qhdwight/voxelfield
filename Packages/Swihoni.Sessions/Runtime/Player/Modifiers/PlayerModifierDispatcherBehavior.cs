using System;
using Swihoni.Components;
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

        public void ModifyChecked(SessionBase session, int playerId, Container playerToModify, Container commands, uint durationUs)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(session, playerId, playerToModify, commands, durationUs);
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

        public void ModifyCommands(SessionBase session, Container commandsToModify)
        {
            if (session.ShouldInterruptCommands)
            {
                // TODO:refactor attribute
                commandsToModify.Require<MouseComponent>().Zero();
                commandsToModify.Require<InputFlagProperty>().Zero();
                return;
            }
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyCommands(session, commandsToModify);
        }

        public void EvaluateHitboxes(SessionBase session, int playerId, Container player) => m_HitboxManager.Evaluate(session, playerId, player);

        public void Dispose()
        {
            if (m_HitboxManager) m_HitboxManager.Dispose();
        }

        public void InterpretCommand(SessionBase session, string stringCommand, int playerId, Container player, Container sessionContainer)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.InterpretCommand(session, stringCommand, playerId, player, sessionContainer);
        }
    }

    public abstract class PlayerModifierBehaviorBase : MonoBehaviour
    {
        protected SessionBase m_Session;

        internal virtual void Setup(SessionBase session) => m_Session = session;

        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        public virtual void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, uint durationUs) => SynchronizeBehavior(player);

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        public virtual void ModifyTrusted(SessionBase session, int playerId, Container trustedPlayer, Container player, Container commands, uint durationUs) =>
            SynchronizeBehavior(trustedPlayer);

        public virtual void ModifyCommands(SessionBase session, Container commands) { }

        protected internal virtual void SynchronizeBehavior(Container player) { }

        protected internal virtual void InterpretCommand(SessionBase sessionBase, string stringCommand, int playerId, Container player, Container sessionContainer) { }
    }
}