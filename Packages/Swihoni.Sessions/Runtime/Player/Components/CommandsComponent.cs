using System;
using Swihoni.Components;
using Swihoni.Sessions.Components;

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
                          UseThree = 12,
                          Ads = 13,
                          Reload = 14,
                          Fly = 15,
                          Throw = 16,
                          DropItem = 17,
                          Respawn = 18,
                          ItemSelectStart = 200,
                          ItemLast = ItemSelectStart + 10;
    }

    [Serializable, NoSerialization]
    public class MouseComponent : ComponentBase
    {
        public FloatProperty mouseDeltaX, mouseDeltaY;
    }

    [Serializable, ClientTrusted]
    public class InputFlagProperty : UIntProperty
    {
        public bool GetInput(int input) => (Value & (1 << input)) != 0;

        public void SetInput(int input, bool enabled)
        {
            if (WithoutValue) Value = 0;
            if (enabled) Value |= (uint) (1 << input);
            else Value &= (uint) ~(1 << input);
        }

        public float GetAxis(int positiveInput, int negativeInput)
            => (GetInput(positiveInput) ? 1.0f : 0.0f) + (GetInput(negativeInput) ? -1.0f : 0.0f);
    }

    [Serializable, ClientTrusted]
    public class WantedItemIndexProperty : ByteProperty
    {
    }
}