using System.Collections.Generic;
using UnityEngine;

namespace Compound
{
    public enum InputType
    {
        Forward,
        Backward,
        Left,
        Right
    }

    public class InputSettings
    {
        private Dictionary<InputType, KeyCode> m_Mapping;

        public KeyCode Get(InputType type)
        {
            return m_Mapping[type];
        }

        public static InputSettings Defaults()
        {
            return new InputSettings
            {
                m_Mapping = new Dictionary<InputType, KeyCode>
                {
                    [InputType.Forward] = KeyCode.W,
                    [InputType.Backward] = KeyCode.S,
                    [InputType.Left] = KeyCode.A,
                    [InputType.Right] = KeyCode.D
                }
            };
        }
    }

    public class InputProvider : SingletonBehavior<InputProvider>
    {
        private InputSettings m_Settings = InputSettings.Defaults();

        public bool GetInput(InputType type)
        {
            return Input.GetKey(m_Settings.Get(type));
        }

        public float GetAxis(InputType positive, InputType negative)
        {
            return (GetInput(positive) ? 1.0f : 0.0f) + (GetInput(negative) ? -1.0f : 0.0f);
        }
    }
}