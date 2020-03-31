using Input;
using UnityEngine;

namespace Session.Player
{
    public class PlayerCamera : PlayerModifierBehaviorBase
    {
        [SerializeField] private float m_Sensitivity = 10.0f;

        internal override void ModifyTrusted(PlayerStateComponent stateToModify, PlayerCommands commands)
        {
            base.ModifyTrusted(stateToModify, commands);
            stateToModify.yaw.Value += commands.mouseDeltaX * m_Sensitivity;
            stateToModify.pitch.Value -= commands.mouseDeltaY * m_Sensitivity;
        }

        internal override void ModifyCommands(PlayerCommands commandsToModify)
        {
            commandsToModify.mouseDeltaX = InputProvider.GetMouseInput(MouseMovement.X);
            commandsToModify.mouseDeltaY = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        protected override void SynchronizeBehavior(PlayerStateComponent stateToApply)
        {
            transform.rotation = Quaternion.AngleAxis(stateToApply.yaw, Vector3.up);
        }
    }
}