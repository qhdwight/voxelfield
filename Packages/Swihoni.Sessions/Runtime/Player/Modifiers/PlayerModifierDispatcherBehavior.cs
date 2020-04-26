using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerModifierDispatcherBehavior : MonoBehaviour
    {
        private PlayerModifierBehaviorBase[] m_Modifiers;

        internal void Setup(SessionBase session)
        {
            m_Modifiers = GetComponents<PlayerModifierBehaviorBase>();
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.Setup(session);
        }

        public void ModifyChecked(Container containerToModify, Container commands, float duration)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(containerToModify, commands, duration);
        }

        public void ModifyTrusted(Container containerToModify, Container commands, float duration)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyTrusted(containerToModify, commands, duration);
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

        public void ModifyCommands(Container commandsToModify)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyCommands(commandsToModify);
        }
    }

    public abstract class PlayerModifierBehaviorBase : MonoBehaviour, IModifier<Container, Container>
    {
        protected SessionBase m_Session;

        internal virtual void Setup(SessionBase session)
        {
            m_Session = session;
        }

        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        public virtual void ModifyChecked(Container containerToModify, Container commands, float duration) { SynchronizeBehavior(containerToModify); }

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        public virtual void ModifyTrusted(Container containerToModify, Container commandsContainer, float duration) { SynchronizeBehavior(containerToModify); }

        public virtual void ModifyCommands(Container commandsToModify) { }

        protected virtual void SynchronizeBehavior(Container containersToApply) { }
    }
}