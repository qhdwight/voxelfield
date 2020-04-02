using Input;
using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    public class PlayerCameraBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private float m_Sensitivity = 10.0f;

        internal override void ModifyTrusted(PlayerComponent componentToModify, PlayerCommands commands)
        {
            base.ModifyTrusted(componentToModify, commands);
            componentToModify.yaw.Value = Mathf.Repeat(componentToModify.yaw + commands.mouseDeltaX * m_Sensitivity, 360.0f);
            componentToModify.pitch.Value = Mathf.Clamp(componentToModify.pitch - commands.mouseDeltaY * m_Sensitivity, -90.0f, 90.0f);
        }

        internal override void ModifyCommands(PlayerCommands commandsToModify)
        {
            commandsToModify.mouseDeltaX = InputProvider.GetMouseInput(MouseMovement.X);
            commandsToModify.mouseDeltaY = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        protected override void SynchronizeBehavior(PlayerComponent componentToApply)
        {
            transform.rotation = Quaternion.AngleAxis(componentToApply.yaw, Vector3.up);
        }
    }
}