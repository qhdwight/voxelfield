using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Config
{
    public static class InputProvider
    {
        public static bool GetInput(byte type) => Input.GetKey(ConfigManagerBase.Active.input.GetKeyCode(type));

        /// <summary>
        /// Should be called in normal Unity Update() methods
        /// </summary>
        /// <returns>If this is the first Unity frame an input is pressed</returns>
        public static bool GetInputDown(byte type) => Input.GetKeyDown(ConfigManagerBase.Active.input.GetKeyCode(type));

        public static float GetAxis(byte positive, byte negative) => (GetInput(positive) ? 1.0f : 0.0f) + (GetInput(negative) ? -1.0f : 0.0f);

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

        public static float GetMouseScrollWheel() => Input.GetAxisRaw("Mouse ScrollWheel");
    }

    public static class InputType
    {
        public const byte Map = 100,
                          ToggleConsole = 101,
                          ConsoleCommand = 102,
                          OpenScoreboard = 103,
                          AutocompleteConsole = 104,
                          PreviousConsoleCommand = 105,
                          NextConsoleCommand = 106,
                          OpenModelSelect = 107,
                          OpenVoxelSelect = 108,
                          Buy = 109;

        public static DualDictionary<byte, string> Names { get; } = new DualDictionary<byte, string>();

        static InputType()
        {
            foreach (FieldInfo field in typeof(InputType).GetFields(BindingFlags.Static | BindingFlags.Public))
                Names.Add((byte) field.GetValue(null), field.Name);
            foreach (FieldInfo field in typeof(PlayerInput).GetFields(BindingFlags.Static | BindingFlags.Public))
                Names.Add((byte) field.GetValue(null), field.Name);
        }
    }

    [Serializable]
    public class KeyCodeProperty : BoxedEnumProperty<KeyCode>
    {
        public KeyCodeProperty() { }
        public KeyCodeProperty(KeyCode value) : base(value) { }
    }

    [Serializable]
    public class InputBindingProperty : DictProperty<ByteProperty, KeyCodeProperty>
    {
        private static ByteProperty _lookupProperty = new ByteProperty();

        public KeyCode GetKeyCode(byte @byte)
        {
            _lookupProperty.Value = @byte;
            return this[_lookupProperty];
        }

        public override StringBuilder AppendValue(StringBuilder builder)
        {
            var afterFirst = false;
            foreach (KeyValuePair<ByteProperty, KeyCodeProperty> pair in m_Map)
            {
                if (afterFirst) builder.Append(", ");
                builder.Append(InputType.Names.GetForward(pair.Key)).Append("=").AppendPropertyValue(pair.Value);
                afterFirst = true;
            }
            return builder;
        }

        public override bool TryParseValue(string stringValue)
        {
            try
            {
                m_Map.Clear();
                string[] pairs = stringValue.Split(new[] {","}, StringSplitOptions.RemoveEmptyEntries);
                foreach (string pair in pairs)
                {
                    var keyProperty = Activator.CreateInstance<ByteProperty>();
                    var valueProperty = Activator.CreateInstance<KeyCodeProperty>();
                    string[] keyAndValue = pair.Split(new[] {"="}, StringSplitOptions.RemoveEmptyEntries);
                    string key = keyAndValue[0].Trim(), value = keyAndValue[1];
                    keyProperty.Value = InputType.Names.GetReverse(key);
                    valueProperty.TryParseValue(value);
                    m_Map.Add(keyProperty, valueProperty);
                }
                WithValue = true;
                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public InputBindingProperty()
        {
            var defaultMap = new Dictionary<byte, KeyCode>
            {
                [PlayerInput.Forward] = KeyCode.W,
                [PlayerInput.Backward] = KeyCode.S,
                [PlayerInput.Left] = KeyCode.A,
                [PlayerInput.Right] = KeyCode.D,
                [PlayerInput.Jump] = KeyCode.Space,
                [PlayerInput.Crouch] = KeyCode.LeftControl,
                [PlayerInput.Sprint] = KeyCode.LeftShift,
                [PlayerInput.Walk] = KeyCode.LeftAlt,
                [PlayerInput.Suicide] = KeyCode.End,
                [PlayerInput.Interact] = KeyCode.E,
                [InputType.Map] = KeyCode.M,
                [PlayerInput.UseOne] = KeyCode.Mouse0,
                [PlayerInput.UseTwo] = KeyCode.Mouse1,
#if UNITY_EDITOR_OSX
                [PlayerInput.UseThree] = KeyCode.C,
#else
                [PlayerInput.UseThree] = KeyCode.Mouse2,
#endif
                [PlayerInput.Reload] = KeyCode.R,
                [PlayerInput.ItemOne] = KeyCode.Alpha1,
                [PlayerInput.ItemTwo] = KeyCode.Alpha2,
                [PlayerInput.ItemThree] = KeyCode.Alpha3,
                [PlayerInput.ItemFour] = KeyCode.Alpha4,
                [PlayerInput.ItemFive] = KeyCode.Alpha5,
                [PlayerInput.ItemSix] = KeyCode.Alpha6,
                [PlayerInput.ItemSeven] = KeyCode.Alpha7,
                [PlayerInput.ItemEight] = KeyCode.Alpha8,
                [PlayerInput.ItemNine] = KeyCode.Alpha9,
                [PlayerInput.ItemTen] = KeyCode.Alpha0,
                [PlayerInput.ItemLast] = KeyCode.Q,
                [PlayerInput.DropItem] = KeyCode.G,
                [PlayerInput.Ads] = KeyCode.Mouse1,
                [InputType.ToggleConsole] = KeyCode.BackQuote,
                [InputType.ConsoleCommand] = KeyCode.Slash,
                [InputType.OpenScoreboard] = KeyCode.Tab,
                [InputType.AutocompleteConsole] = KeyCode.Tab,
                [InputType.PreviousConsoleCommand] = KeyCode.UpArrow,
                [InputType.NextConsoleCommand] = KeyCode.DownArrow,
                [PlayerInput.Fly] = KeyCode.F,
                [InputType.Buy] = KeyCode.B,
                [PlayerInput.Throw] = KeyCode.G,
                [InputType.OpenModelSelect] = KeyCode.M,
                [InputType.OpenVoxelSelect] = KeyCode.V,
                [PlayerInput.Respawn] = KeyCode.Return
            };
            m_Map = new Dictionary<ByteProperty, KeyCodeProperty>();
            foreach (KeyValuePair<byte, KeyCode> pair in defaultMap)
                m_Map.Add(new ByteProperty(pair.Key), new KeyCodeProperty(pair.Value));
        }
    }

    public enum MouseMovement
    {
        X,
        Y
    }
}