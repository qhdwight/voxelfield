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
                          UseFour = 13,
                          Ads = 14,
                          Reload = 15,
                          Fly = 16,
                          Throw = 17,
                          DropItem = 18,
                          Respawn = 19,
                          ItemOne = 20,
                          ItemTwo = 21,
                          ItemThree = 22,
                          ItemFour = 23,
                          ItemFive = 24,
                          ItemSix = 25,
                          ItemSeven = 26,
                          ItemEight = 27,
                          ItemNine = 28,
                          ItemTen = 29,
                          ItemLast = 30;
    }

    [Serializable, NoSerialization]
    public class MouseComponent : ComponentBase
    {
        public FloatProperty mouseDeltaX, mouseDeltaY;
    }

    [Serializable, ClientTrusted, SingleTick(true)]
    public class InputFlagProperty : UIntProperty
    {
        public bool GetInput(int input) => (Value & (1 << input)) != 0;

        public void SetInput(int input, bool enabled)
        {
            SetValueIfWithout();
            if (enabled) Value |= (uint) (1 << input);
            else Value &= (uint) ~(1 << input);
        }

        public void SetInput(int input)
        {
            SetValueIfWithout();
            Value |= (uint) (1 << input);
        }

        public float GetAxis(int positiveInput, int negativeInput) => (GetInput(positiveInput) ? 1.0f : 0.0f) + (GetInput(negativeInput) ? -1.0f : 0.0f);
    }

    [Serializable, ClientTrusted]
    public class WantedItemIndexProperty : ByteProperty
    {
    }
}