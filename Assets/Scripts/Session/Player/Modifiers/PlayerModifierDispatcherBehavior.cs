using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    using PlayerModifier = ModifierBehaviorBase<PlayerComponent>;
    
    [RequireComponent(typeof(PlayerCameraBehavior), typeof(PlayerMovement))]
    public class PlayerModifierDispatcherBehavior : MonoBehaviour
    {
        private PlayerModifier[] m_Modifiers;

        internal void Setup()
        {
            m_Modifiers = GetComponents<PlayerModifier>();
            foreach (PlayerModifier modifier in m_Modifiers) modifier.Setup();
        }

        internal void ModifyChecked(PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            foreach (PlayerModifier modifier in m_Modifiers) modifier.ModifyChecked(componentToModify, commands);
        }

        internal void ModifyTrusted(PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            foreach (PlayerModifier modifier in m_Modifiers) modifier.ModifyTrusted(componentToModify, commands);
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
        internal void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
            foreach (PlayerModifier modifier in m_Modifiers) modifier.ModifyCommands(commandsToModify);
        }
    }
}