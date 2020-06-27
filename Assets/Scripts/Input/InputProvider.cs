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
        Crouch,
        Sprint,
        Walk,
        Interact,
        Map,
        Suicide,
        UseOne,
        UseTwo,
        Reload,
        ItemOne,
        ItemTwo,
        ItemThree,
        ItemFour,
        ItemFive,
        ItemSix,
        ItemSeven,
        ItemEight,
        ItemNine,
        Ads,
        ToggleConsole,
        OpenScoreboard,
        AutocompleteConsole,
        PreviousConsoleCommand,
        NextConsoleCommand,
        Fly
    }

    public enum MouseMovement
    {
        X,
        Y
    }

    public class InputSettings
    {
        private Dictionary<InputType, KeyCode> m_Mapping;

        public KeyCode Get(InputType type) => m_Mapping[type];

        public static InputSettings Defaults() =>
            new InputSettings
            {
                m_Mapping = new Dictionary<InputType, KeyCode>
                {
                    [InputType.Forward] = KeyCode.W,
                    [InputType.Backward] = KeyCode.S,
                    [InputType.Left] = KeyCode.A,
                    [InputType.Right] = KeyCode.D,
                    [InputType.Jump] = KeyCode.Space,
                    [InputType.Crouch] = KeyCode.LeftControl,
                    [InputType.Sprint] = KeyCode.LeftShift,
                    [InputType.Walk] = KeyCode.LeftAlt,
                    [InputType.Suicide] = KeyCode.End,
                    [InputType.Interact] = KeyCode.E,
                    [InputType.Map] = KeyCode.M,
                    [InputType.UseOne] = KeyCode.Mouse0,
                    [InputType.UseTwo] = KeyCode.Mouse2,
                    [InputType.Reload] = KeyCode.R,
                    [InputType.ItemOne] = KeyCode.Alpha1,
                    [InputType.ItemTwo] = KeyCode.Alpha2,
                    [InputType.ItemThree] = KeyCode.Alpha3,
                    [InputType.ItemFour] = KeyCode.Alpha4,
                    [InputType.ItemFive] = KeyCode.Alpha5,
                    [InputType.ItemSix] = KeyCode.Alpha6,
                    [InputType.ItemSeven] = KeyCode.Alpha7,
                    [InputType.ItemEight] = KeyCode.Alpha8,
                    [InputType.ItemNine] = KeyCode.Alpha9,
                    [InputType.Ads] = KeyCode.Mouse1,
                    [InputType.ToggleConsole] = KeyCode.BackQuote,
                    [InputType.OpenScoreboard] = KeyCode.Tab,
                    [InputType.AutocompleteConsole] = KeyCode.Tab,
                    [InputType.PreviousConsoleCommand] = KeyCode.UpArrow,
                    [InputType.NextConsoleCommand] = KeyCode.DownArrow,
                    [InputType.Fly] = KeyCode.F
                }
            };
    }

    public class InputProvider : SingletonBehavior<InputProvider>
    {
        private InputSettings m_Settings = InputSettings.Defaults();

        public float Sensitivity { get; set; } = 2.0f;

        public bool GetInput(InputType type) => UnityEngine.Input.GetKey(m_Settings.Get(type));

        /// <summary>
        /// Should be called in normal Unity Update() methods
        /// </summary>
        /// <returns>If this is the first Unity frame an input is pressed</returns>
        public bool GetInputDown(InputType type) => UnityEngine.Input.GetKeyDown(m_Settings.Get(type));

        public float GetAxis(InputType positiveKey, InputType negativeKey) => (GetInput(positiveKey) ? 1.0f : 0.0f) + (GetInput(negativeKey) ? -1.0f : 0.0f);

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

        public static float GetMouseScrollWheel() => UnityEngine.Input.GetAxisRaw("Mouse ScrollWheel");
    }
}