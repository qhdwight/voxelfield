using System;
using System.Collections.Generic;
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

        private PlayerMovement m_Movement;
        public PlayerMovement Movement
        {
            get
            {
                if (m_Movement == null) m_Movement = GetComponentInChildren<PlayerMovement>();
                return m_Movement;
            }
        }

        internal void Setup(SessionBase session, int playerId)
        {
            m_Modifiers = GetComponents<PlayerModifierBehaviorBase>();
            m_HitboxManager = GetComponent<PlayerHitboxManager>();
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.Setup();
            if (m_HitboxManager) m_HitboxManager.Setup();
            m_Trigger = GetComponentInChildren<PlayerTrigger>(true);
            if (m_Trigger) m_Trigger.Setup(playerId);
        }

        public void ModifyChecked(in SessionContext context)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(context);

            bool isOnServer = context.player.With<ServerTag>();

            if (isOnServer && context.player.With(out MoveComponent move))
                context.session.Injector.OnServerMove(context, move);

            if (isOnServer && context.player.WithPropertyWithValue(out FlashProperty flash))
            {
                flash.Value -= context.durationUs / 2_000_000f;
                if (flash < 0.0f) flash.Clear();
            }

            if (context.WithServerStringCommands(out IEnumerable<string[]> stringCommands))
                foreach (string[] stringCommand in stringCommands)
                    DefaultConfig.TryHandleArguments(stringCommand);

            if (isOnServer && context.sessionContainer.With(out ChatList chats)
                           && context.commands.WithPropertyWithValue(out ChatEntryProperty chat))
            {
                var namedChat = new ChatEntryProperty();
                namedChat.Builder.Append(context.playerId).Append(" ").AppendPropertyValue(chat);
                chats.Append(namedChat);
            }
        }

        public void ModifyTrusted(in SessionContext context, Container verifiedPlayer)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyTrusted(context, verifiedPlayer);
        }

        public void Synchronize(in SessionContext context)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.SynchronizeBehavior(context);
        }

        public void ModifyCommands(SessionBase session, Container commandsToModify, int playerId)
        {
            if (SessionBase.InterruptingInterface)
            {
                // TODO:refactor attribute
                commandsToModify.ZeroIfWith<MouseComponent>();
                commandsToModify.ZeroIfWith<InputFlagProperty>();
                return;
            }
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyCommands(session, commandsToModify, playerId);
        }

        public void EvaluateHitboxes(in SessionContext context) => m_HitboxManager.Evaluate(context);

        public void Dispose()
        {
            if (m_HitboxManager) m_HitboxManager.Dispose();
        }
    }

    public abstract class PlayerModifierBehaviorBase : MonoBehaviour
    {
        internal virtual void Setup() { }

        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        public virtual void ModifyChecked(in SessionContext context) => SynchronizeBehavior(context);

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        public virtual void ModifyTrusted(in SessionContext context, Container verifiedPlayer) => SynchronizeBehavior(context);

        public virtual void ModifyCommands(SessionBase session, Container commands, int playerId) { }

        protected internal virtual void SynchronizeBehavior(in SessionContext context) { }
    }
}