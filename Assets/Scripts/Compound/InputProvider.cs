using System;
using System.Collections.Generic;
using UnityEngine;

namespace Compound
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
            return Input.GetKey(m_Settings.Get(type));
        }

        public float GetAxis(InputType positive, InputType negative)
        {
            return (GetInput(positive) ? 1.0f : 0.0f) + (GetInput(negative) ? -1.0f : 0.0f);
        }

        public static float GetMouseInput(MouseMovement mouseMovement)
        {
            switch (mouseMovement)
            {
                case MouseMovement.X:
                    return Input.GetAxisRaw("Mouse X");
                case MouseMovement.Y:
                    return Input.GetAxisRaw("Mouse Y");
                default:
                    throw new ArgumentOutOfRangeException(nameof(mouseMovement), mouseMovement, null);
            }
        }

        public static float GetMouseScrollWheel()
        {
            return Input.GetAxisRaw("Mouse ScrollWheel");
        }
    }
}