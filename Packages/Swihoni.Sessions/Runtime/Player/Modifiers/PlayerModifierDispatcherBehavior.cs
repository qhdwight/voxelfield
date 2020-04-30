using System;
using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerModifierDispatcherBehavior : MonoBehaviour, IDisposable
    {
        private PlayerModifierBehaviorBase[] m_Modifiers;
        private PlayerHitboxManager m_HitboxManager;
        private SessionBase m_Session;

        internal void Setup(SessionBase session)
        {
            m_Session = session;
            m_Modifiers = GetComponents<PlayerModifierBehaviorBase>();
            m_HitboxManager = GetComponent<PlayerHitboxManager>();
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.Setup(session);
            if (m_HitboxManager) m_HitboxManager.Setup(session);
        }

        public void ModifyChecked(SessionBase session, int playerId, Container playerToModify, Container commands, float duration)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(session, playerId, playerToModify, commands, duration);
        }

        public void ModifyTrusted(SessionBase session, int playerId, Container playerToModify, Container commands, float duration)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyTrusted(session, playerId, playerToModify, commands, duration);
        }

        public void Synchronize(Container player)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.SynchronizeBehavior(player);
        }

        // public List<TComponent> GetInterfaces<TComponent>() where TComponent : class
        // {
        //     var components = new List<TComponent>();
        //     foreach (Component component in GetComponents<Component>())
        //     {
        //         if (component is TComponent subComponent)
        //             components.Add(subComponent);
        //     }
        //     return components;
        // }

        public void ModifyCommands(SessionBase session, Container commandsToModify)
        {
            if (m_Session.ShouldInterruptCommands) return;
            
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyCommands(session, commandsToModify);
        }

        public void EvaluateHitboxes(int playerId, Container player) => m_HitboxManager.Evaluate(playerId, player);

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
        public virtual void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, float duration) => SynchronizeBehavior(player);

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        public virtual void ModifyTrusted(SessionBase session, int playerId, Container player, Container commands, float duration) => SynchronizeBehavior(player);

        public virtual void ModifyCommands(SessionBase session, Container commands) { }

        internal virtual void SynchronizeBehavior(Container player) { }
    }
}