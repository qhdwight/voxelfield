using System;
using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerModifierDispatcherBehavior : MonoBehaviour, IDisposable
    {
        private PlayerModifierBehaviorBase[] m_Modifiers;
        private PlayerHitboxManager m_HitboxManager;

        internal void Setup(SessionBase session)
        {
            m_Modifiers = GetComponents<PlayerModifierBehaviorBase>();
            m_HitboxManager = GetComponent<PlayerHitboxManager>();
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.Setup();
            if (m_HitboxManager) m_HitboxManager.Setup(session);
        }

        public void ModifyChecked(SessionBase session, int playerId, Container containerToModify, Container commands, float duration)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(session, playerId, containerToModify, commands, duration);
        }

        public void ModifyTrusted(SessionBase session, int playerId, Container containerToModify, Container commands, float duration)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyTrusted(session, playerId, containerToModify, commands, duration);
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
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyCommands(session, commandsToModify);
        }

        public void EvaluateHitboxes(int playerId, Container player) { m_HitboxManager.Evaluate(playerId, player); }

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
        public virtual void ModifyChecked(SessionBase session, int playerId, Container player, Container commands, float duration) { SynchronizeBehavior(player); }

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        public virtual void ModifyTrusted(SessionBase session, int playerId, Container player, Container commands, float duration) { SynchronizeBehavior(player); }

        public virtual void ModifyCommands(SessionBase session, Container commands) { }

        protected virtual void SynchronizeBehavior(Container player) { }
    }
}