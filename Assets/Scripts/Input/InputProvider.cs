using System;
using System.Collections.Generic;
using Swihoni.Util;
using UnityEngine;

namespace Input
{
    public enum InputType
    {
        Forward,
        Backward,
        Left,
        Right,
        Jump,
        UseOne,
        UseTwo,
        Reload,
        ItemOne,
        ItemTwo,
        ItemThree,
        Ads,
        ToggleConsole,
        AutocompleteConsole,
        PreviousConsoleCommand,
        NextConsoleCommand
    }

    public enum MouseMovement
    {
        X,
        Y
    }

    public class InputSettings
    {
        private Dictionary<InputType, KeyCode> m_Mapping;

        public KeyCode Get(InputType type) { return m_Mapping[type]; }

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
                    [InputType.Jump] = KeyCode.Space,
                    [InputType.UseOne] = KeyCode.Mouse0,
                    [InputType.UseTwo] = KeyCode.Mouse2,
                    [InputType.Reload] = KeyCode.R,
                    [InputType.ItemOne] = KeyCode.Alpha1,
                    [InputType.ItemTwo] = KeyCode.Alpha2,
                    [InputType.ItemThree] = KeyCode.Alpha3,
                    [InputType.Ads] = KeyCode.Mouse1,
                    [InputType.ToggleConsole] = KeyCode.BackQuote,
                    [InputType.AutocompleteConsole] = KeyCode.Tab,
                    [InputType.PreviousConsoleCommand] = KeyCode.UpArrow,
                    [InputType.NextConsoleCommand] = KeyCode.DownArrow,
                }
            };
        }
    }

    public class InputProvider : SingletonBehavior<InputProvider>
    {
        private InputSettings m_Settings = InputSettings.Defaults();

        public bool GetInput(InputType type) { return UnityEngine.Input.GetKey(m_Settings.Get(type)); }

        /// <summary>
        ///     Should be called in normal Unity Update() methods
        /// </summary>
        /// <returns>If this is the first Unity frame an input is pressed</returns>
        public bool GetInputDown(InputType type) { return UnityEngine.Input.GetKeyDown(m_Settings.Get(type)); }

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

        public static float GetMouseScrollWheel() { return UnityEngine.Input.GetAxisRaw("Mouse ScrollWheel"); }
    }
}