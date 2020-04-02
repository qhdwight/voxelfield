using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    [RequireComponent(typeof(PlayerCameraBehavior), typeof(PlayerMovement))]
    public class PlayerModifierDispatcherBehavior : MonoBehaviour
    {
        private PlayerModifierBehaviorBase[] m_Modifiers;

        internal void Setup()
        {
            m_Modifiers = GetComponents<PlayerModifierBehaviorBase>();
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.Setup();
        }

        internal void ModifyChecked(PlayerComponent componentToModify, PlayerCommands commands)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyChecked(componentToModify, commands);
        }

        internal void ModifyTrusted(PlayerComponent componentToModify, PlayerCommands commands)
        {
            foreach (PlayerModifierBehaviorBase modifier in m_Modifiers) modifier.ModifyTrusted(componentToModify, commands);
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