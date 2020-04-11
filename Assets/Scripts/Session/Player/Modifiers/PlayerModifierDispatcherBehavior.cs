using Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    public class PlayerModifierDispatcherBehavior : MonoBehaviour
    {
        private PlayerModifierBehaviorBase[] m_Modifiers;

        internal void Setup()
        {
            m_Modifiers = GetComponents<PlayerModifierBehaviorBase>();
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.Setup();
        }

        internal void ModifyChecked(ContainerBase containerToModify, ContainerBase commands, float duration)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(containerToModify, commands, duration);
        }

        internal void ModifyTrusted(ContainerBase containerToModify, ContainerBase commands, float duration)
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

        internal void ModifyCommands(ContainerBase commandsToModify)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyCommands(commandsToModify);
        }
    }

    public abstract class PlayerModifierBehaviorBase : MonoBehaviour, IModifierBase<ContainerBase, ContainerBase>
    {
        internal virtual void Setup()
        {
        }

        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        public virtual void ModifyChecked(ContainerBase containerToModify, ContainerBase commands, float duration)
        {
            SynchronizeBehavior(containerToModify);
        }

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        public virtual void ModifyTrusted(ContainerBase containerToModify, ContainerBase commandsContainer, float duration)
        {
            SynchronizeBehavior(containerToModify);
        }

        public virtual void ModifyCommands(ContainerBase commandsToModify)
        {
        }

        protected virtual void SynchronizeBehavior(ContainerBase containersToApply)
        {
        }
    }
}