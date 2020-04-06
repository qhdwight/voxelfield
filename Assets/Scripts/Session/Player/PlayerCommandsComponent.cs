using System;
using Components;

namespace Session.Player
{
    public enum PlayerInput
    {
        Forward,
        Backward,
        Right,
        Left,
        Jump,
        UseOne,
        UseTwo,
        Ads,
        Reload
    }

    [Serializable]
    public class PlayerCommandsComponent : ComponentBase
    {
        public FloatProperty duration;
        public UShortProperty inputs;
        public ByteProperty wantedItemIndex;
        public FloatProperty mouseDeltaX, mouseDeltaY;

        public bool GetInput(PlayerInput input) => (inputs & (1 << (int) input)) != 0;

        public float GetAxis(PlayerInput positiveInput, PlayerInput negativeInput) =>
            (GetInput(positiveInput) ? 1.0f : 0.0f) + (GetInput(negativeInput) ? -1.0f : 0.0f);

        public void SetInput(PlayerInput input, bool enabled)
        {
            if (enabled)
                inputs.Value |= (ushort) (1 << (int) input);
            else
                inputs.Value &= (ushort) ~(1 << (int) input);
        }
    }
}