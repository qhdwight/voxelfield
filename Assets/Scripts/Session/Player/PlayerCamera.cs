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
            stateToModify.yaw.Value = Mathf.Repeat(stateToModify.yaw + commands.mouseDeltaX * m_Sensitivity, 360.0f);
            stateToModify.pitch.Value = Mathf.Clamp(stateToModify.pitch - commands.mouseDeltaY * m_Sensitivity, -90.0f, 90.0f);
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