using Components;
using UnityEngine;

namespace Session.Player
{
    public class PlayerStateComponent : ComponentBase
    {
        [NoInterpolate] public Property<byte> health;
        public Property<Vector3> position;
        public Property<float> yaw, pitch;
    }
}