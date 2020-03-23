using System.Collections.Generic;
using UnityEngine;

namespace Session.Player
{
    internal interface IPlayerModifier
    {
        void Modify(PlayerData data, PlayerCommands commands);
    }

    public class PlayerModifierBehavior : MonoBehaviour, IPlayerModifier
    {
        private List<IPlayerModifier> m_Modifiers;

        private void Awake()
        {
            m_Modifiers = GetInterfaces<IPlayerModifier>();
            m_Modifiers.Remove(this);
        }

        public void Modify(PlayerData data, PlayerCommands commands)
        {
            foreach (IPlayerModifier modifier in m_Modifiers)
            {
                modifier.Modify(data, commands);
            }
        }

        private List<TComponent> GetInterfaces<TComponent>() where TComponent : class
        {
            var components = new List<TComponent>();
            foreach (Component component in GetComponents<Component>())
            {
                if (component is TComponent subComponent)
                    components.Add(subComponent);
            }
            return components;
        }
    }
}