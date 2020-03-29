using UnityEngine;

namespace Session.Player
{
    [RequireComponent(typeof(PlayerCamera), typeof(PlayerMovement))]
    public class PlayerModifierDispatcherBehavior : MonoBehaviour
    {
        private PlayerModifierBehaviorBase[] m_Modifiers;

        public void Setup()
        {
            m_Modifiers = GetComponents<PlayerModifierBehaviorBase>();
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.Setup();
        }

        internal void ModifyChecked(PlayerState stateToModify, PlayerCommands commands)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(stateToModify, commands);
        }

        internal void ModifyTrusted(PlayerState stateToModify, PlayerCommands commands)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyTrusted(stateToModify, commands);
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
        internal void ModifyCommands(PlayerCommands commandsToModify)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyCommands(commandsToModify);
        }
    }
}