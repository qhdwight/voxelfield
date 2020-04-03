using System;
using Components;

namespace Session.Player
{
    [Serializable]
    public class PlayerCommandsComponent : ComponentBase
    {
        public Property<float> duration;
        public Property<bool> jumpInput;
        public Property<float> mouseDeltaX, mouseDeltaY;
        public Property<float> vInput, hInput;
    }
}