using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Components;
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
        UseThree,
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
        ItemTen,
        ItemLast,
        Ads,
        ToggleConsole,
        ConsoleCommand,
        OpenScoreboard,
        AutocompleteConsole,
        PreviousConsoleCommand,
        NextConsoleCommand,
        Fly,
        Buy,
        Throw,
        OpenModelSelect,
        OpenVoxelSelect,
    }

    [Serializable]
    public class KeyCodeProperty : PropertyBase<KeyCode>
    {
        public override bool ValueEquals(PropertyBase<KeyCode> other) => other.Value == Value;
        public override void DeserializeValue(NetDataReader reader) => Value = (KeyCode) reader.GetUShort();
        public override void SerializeValue(NetDataWriter writer) => writer.Put((ushort) Value);

        public override bool TryParseValue(string @string)
        {
            if (Enum.TryParse(@string, out KeyCode keyCode))
            {
                Value = keyCode;
                return true;
            }
            return false;
        }
    }

    [Serializable]
    public class InputComponent
    {
        public DictProperty<ByteProperty, KeyCodeProperty> bindings;
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
                    [InputType.UseTwo] = KeyCode.Mouse1,
#if UNITY_EDITOR_OSX
                    [InputType.UseThree] = KeyCode.C,
#else
                    [InputType.UseThree] = KeyCode.Mouse2,
#endif
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
                    [InputType.ItemTen] = KeyCode.Alpha0,
                    [InputType.ItemLast] = KeyCode.Q,
                    [InputType.Ads] = KeyCode.Mouse1,
                    [InputType.ToggleConsole] = KeyCode.BackQuote,
                    [InputType.ConsoleCommand] = KeyCode.Slash,
                    [InputType.OpenScoreboard] = KeyCode.Tab,
                    [InputType.AutocompleteConsole] = KeyCode.Tab,
                    [InputType.PreviousConsoleCommand] = KeyCode.UpArrow,
                    [InputType.NextConsoleCommand] = KeyCode.DownArrow,
                    [InputType.Fly] = KeyCode.F,
                    [InputType.Buy] = KeyCode.B,
                    [InputType.Throw] = KeyCode.G,
                    [InputType.OpenModelSelect] = KeyCode.M,
                    [InputType.OpenVoxelSelect] = KeyCode.V
                }
            };
    }

    public class InputProvider : SingletonBehavior<InputProvider>
    {
        private InputSettings m_Settings = InputSettings.Defaults();

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