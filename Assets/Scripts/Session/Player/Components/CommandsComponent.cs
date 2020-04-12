using System;
using Components;

namespace Session.Player.Components
{
    public static class PlayerInput
    {
        public const byte Forward = 0,
                          Backward = 1,
                          Right = 2,
                          Left = 3,
                          Jump = 4,
                          UseOne = 5,
                          UseTwo = 6,
                          Ads = 7,
                          Reload = 8,
                          Last = Reload;
    }

    [Serializable]
    public class MouseComponent : ComponentBase
    {
        [NoSerialization] public FloatProperty mouseDeltaX, mouseDeltaY;
    }

    [Serializable]
    public class InputFlagProperty : UShortProperty
    {
        public bool GetInput(int input) => (Value & (1 << input)) != 0;

        public void SetInput(int input, bool enabled)
        {
            if (enabled)
                Value |= (ushort) (1 << input);
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