using System;
using System.Collections.Generic;
using UnityEngine;
using Util;

namespace Input
{
    public enum InputType
    {
        Forward,
        Backward,
        Left,
        Right,
        Jump
    }

    public enum MouseMovement
    {
        X,
        Y
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
                    [InputType.Right] = KeyCode.D,
                    [InputType.Jump] = KeyCode.Space
                }
            };
        }
    }

    public class InputProvider : SingletonBehavior<InputProvider>
    {
        private InputSettings m_Settings = InputSettings.Defaults();

        public bool GetInput(InputType type)
        {
            return UnityEngine.Input.GetKey(m_Settings.Get(type));
        }
        
        public static float GetMouseInput(MouseMovement mouseMovement)
        {
            switch (mouseMovement)
            {
                case MouseMovement.X:
                    return UnityEngine.Input.GetAxisRaw("Mouse X");
                case MouseMovement.Y:
                    return UnityEngine.Input.GetAxisRaw("Mouse Y");
                default:
                    throw new ArgumentOutOfRangeException(nameof(mouseMovement), mouseMovement, null);
            }
        }

        public static float GetMouseScrollWheel()
        {
            return UnityEngine.Input.GetAxisRaw("Mouse ScrollWheel");
        }
    }
}