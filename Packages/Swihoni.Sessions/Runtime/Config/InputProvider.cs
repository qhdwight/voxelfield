using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Config
{
    public static class InputProvider
    {
        public static bool GetInput(byte type) => Input.GetKey(ConfigManagerBase.Singleton.input.GetKeyCode(type));

        /// <summary>
        /// Should be called in normal Unity Update() methods
        /// </summary>
        /// <returns>If this is the first Unity frame an input is pressed</returns>
        public static bool GetInputDown(byte type) => Input.GetKeyDown(ConfigManagerBase.Singleton.input.GetKeyCode(type));

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
    }

    [Serializable]
    public class KeyCodeProperty : PropertyBase<KeyCode>
    {
        public KeyCodeProperty() { }
        public KeyCodeProperty(KeyCode value) : base(value) { }

        public override bool ValueEquals(in KeyCode value) => value == Value;
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
    public class InputComponent : ComponentBase
    {
        private static ByteProperty _lookupProperty = new ByteProperty();

        public DictProperty<ByteProperty, KeyCodeProperty> bindings;

        public KeyCode GetKeyCode(byte @byte)
        {
            _lookupProperty.Value = @byte;
            return bindings[_lookupProperty];
        }

        public InputComponent()
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
                [InputType.UseThree] = KeyCode.C,
#else
                [PlayerInput.UseThree] = KeyCode.Mouse2,
#endif
                [PlayerInput.Reload] = KeyCode.R,
                [PlayerInput.ItemSelectStart] = KeyCode.Alpha1,
                [PlayerInput.ItemSelectStart + 1] = KeyCode.Alpha2,
                [PlayerInput.ItemSelectStart + 2] = KeyCode.Alpha3,
                [PlayerInput.ItemSelectStart + 3] = KeyCode.Alpha4,
                [PlayerInput.ItemSelectStart + 4] = KeyCode.Alpha5,
                [PlayerInput.ItemSelectStart + 5] = KeyCode.Alpha6,
                [PlayerInput.ItemSelectStart + 6] = KeyCode.Alpha7,
                [PlayerInput.ItemSelectStart + 7] = KeyCode.Alpha8,
                [PlayerInput.ItemSelectStart + 8] = KeyCode.Alpha9,
                [PlayerInput.ItemSelectStart + 9] = KeyCode.Alpha0,
                [PlayerInput.ItemSelectStart + 10] = KeyCode.Q,
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
            bindings = new DictProperty<ByteProperty, KeyCodeProperty>();
            foreach (KeyValuePair<byte, KeyCode> pair in defaultMap)
                bindings.Set(new ByteProperty(pair.Key), new KeyCodeProperty(pair.Value));
        }
    }

    public enum MouseMovement
    {
        X,
        Y
    }
}