using System;
using Swihoni.Components;

namespace Swihoni.Sessions.Player.Components
{
    public static class PlayerInput
    {
        public const byte Forward = 0,
                          Backward = 1,
                          Right = 2,
                          Left = 3,
                          Jump = 4,
                          Crouch = 5,
                          Sprint = 6,
                          Walk = 7,
                          Interact = 8,
                          Suicide = 9,
                          UseOne = 10,
                          UseTwo = 11,
                          Ads = 12,
                          Reload = 13,
                          Last = Reload;
    }

    [Serializable, NoSerialization]
    public class MouseComponent : ComponentBase
    {
        public FloatProperty mouseDeltaX, mouseDeltaY;
    }

    [Serializable]
    public class InputFlagProperty : UShortProperty
    {
        public bool GetInput(int input) => (Value & (1 << input)) != 0;

        public void SetInput(int input, bool enabled)
        {
            if (!HasValue) Value = 0;
            if (enabled) Value |= (ushort) (1 << input);
            else Value &= (ushort) ~(1 << input);
        }

        public float GetAxis(int positiveInput, int negativeInput) =>
            (GetInput(positiveInput) ? 1.0f : 0.0f) + (GetInput(negativeInput) ? -1.0f : 0.0f);
    }

    [Serializable]
    public class WantedItemIndexProperty : ByteProperty
    {
    }
}