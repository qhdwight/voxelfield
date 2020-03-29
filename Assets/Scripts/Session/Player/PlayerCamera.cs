using Compound;
using UnityEngine;

namespace Session.Player
{
    public class PlayerCamera : PlayerModifierBehaviorBase
    {
        [SerializeField] private float m_Sensitivity = 10.0f;

        internal override void ModifyTrusted(PlayerState stateToModify, PlayerCommands commands)
        {
            stateToModify.yaw += commands.mouseDeltaX * m_Sensitivity;
            stateToModify.pitch -= commands.mouseDeltaY * m_Sensitivity;
            transform.rotation = Quaternion.AngleAxis(stateToModify.yaw, Vector3.up);
        }

        internal override void ModifyCommands(PlayerCommands commandsToModify)
        {
            commandsToModify.mouseDeltaX = InputProvider.GetMouseInput(MouseMovement.X);
            commandsToModify.mouseDeltaY = InputProvider.GetMouseInput(MouseMovement.Y);
        }
    }
}