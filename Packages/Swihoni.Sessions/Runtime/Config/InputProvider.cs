using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Config
{
    public static class InputProvider
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool GetInput(byte type) => Input.GetKey(GetKeyCode(type));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static KeyCode GetKeyCode(byte type) => DefaultConfig.Active.inputBindings.GetKeyCode(type);

        /// <summary>
        /// Should be called in normal Unity Update() methods
        /// </summary>
        /// <returns>If this is the first Unity frame an input is pressed</returns>
        public static bool GetInputDown(byte type) => Input.GetKeyDown(DefaultConfig.Active.inputBindings.GetKeyCode(type));

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

        public static float GetMouseScrollWheel() => Input.GetAxisRaw("Mouse ScrollWheel") * (DefaultConfig.Active.invertScrollWheel ? -1.0f : 1.0f);
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
                          OpenContext = 107,
                          SwitchTeams = 108,
                          Buy = 109,
                          TogglePauseMenu = 110,
                          ToggleChat = 111,
                          NextSpectating = 112,
                          PreviousSpectating = 113;

        public static DualDictionary<byte, string> Names { get; } = new();

        static InputType()
        {
            foreach (FieldInfo field in new[] { typeof(InputType), typeof(PlayerInput) }.SelectMany(t => t.GetFields(BindingFlags.Static | BindingFlags.Public)))
                Names.Add((byte)field.GetValue(null), field.Name.ToSnakeCase());
        }

        public static StringBuilder AppendInputKey(this StringBuilder builder, byte input)
            => builder.Append(KeyCodeProperty.DisplayNames.GetForward(InputProvider.GetKeyCode(input)));
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
        private const string Separator = ", ";

        private static ByteProperty _lookupProperty = new();
        private static Dictionary<byte, KeyCode> _defaultMap = new()
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
            [PlayerInput.UseFour] = KeyCode.Mouse3,
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
            [InputType.SwitchTeams] = KeyCode.Period,
            [InputType.OpenScoreboard] = KeyCode.Tab,
            [InputType.AutocompleteConsole] = KeyCode.Tab,
            [InputType.PreviousConsoleCommand] = KeyCode.UpArrow,
            [InputType.NextConsoleCommand] = KeyCode.DownArrow,
            [PlayerInput.Fly] = KeyCode.F,
            [InputType.Buy] = KeyCode.B,
            [PlayerInput.Throw] = KeyCode.G,
            [InputType.OpenContext] = KeyCode.V,
            [InputType.TogglePauseMenu] = KeyCode.Escape,
            [InputType.ToggleChat] = KeyCode.T,
            [PlayerInput.Respawn] = KeyCode.Return,
            [InputType.NextSpectating] = KeyCode.Mouse1,
            [InputType.PreviousSpectating] = KeyCode.Mouse0
        };

        public KeyCode GetKeyCode(byte @byte)
        {
            _lookupProperty.Value = @byte;
            return m_Map.TryGetValue(_lookupProperty, out KeyCodeProperty keyCode) ? keyCode : KeyCode.None;
        }

        public override StringBuilder AppendValue(StringBuilder builder)
        {
            var afterFirst = false;
            foreach ((ByteProperty input, KeyCodeProperty keyCode) in m_Map)
            {
                if (afterFirst) builder.Append(Separator);
                builder.Append(InputType.Names.GetForward(input)).Append("=").AppendPropertyValue(keyCode);
                afterFirst = true;
            }
            return builder;
        }

        public override void ParseValue(string stringValue)
        {
            Zero();
            string[] pairs = stringValue.Split(new[] { Separator}, StringSplitOptions.RemoveEmptyEntries);
            foreach (string pair in pairs)
            {
                var keyProperty = new ByteProperty();
                var valueProperty = new KeyCodeProperty();
                string[] keyAndValue = pair.Split(new[] { "=" }, StringSplitOptions.RemoveEmptyEntries);
                string key = keyAndValue[0].Trim(), value = keyAndValue[1];
                keyProperty.Value = InputType.Names.GetReverse(key);
                valueProperty.ParseValue(value);
                Set(keyProperty, valueProperty);
            }
            foreach ((byte _input, KeyCode keyCode) in _defaultMap)
            {
                var input = new ByteProperty(_input);
                if (m_Map.ContainsKey(input)) continue;

                Set(input, new KeyCodeProperty(keyCode));
                Debug.LogWarning($"Had to add default input for {InputType.Names.GetForward(input)}");
            }
        }

        public InputBindingProperty()
        {
            m_Map = new Dictionary<ByteProperty, KeyCodeProperty>();
            foreach ((byte input, KeyCode keyCode) in _defaultMap)
                m_Map.Add(new ByteProperty(input), new KeyCodeProperty(keyCode));
            WithValue = true;
        }
    }

    public enum MouseMovement
    {
        X,
        Y
    }
}