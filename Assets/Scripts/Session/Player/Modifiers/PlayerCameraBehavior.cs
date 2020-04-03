using Input;
using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    public class PlayerCameraBehavior : ModifierBehaviorBase<PlayerComponent>
    {
        [SerializeField] private float m_Sensitivity = 10.0f;
        
        public override void ModifyTrusted(PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            base.ModifyTrusted(componentToModify, commands);
            componentToModify.yaw.Value = Mathf.Repeat(componentToModify.yaw + commands.mouseDeltaX * m_Sensitivity, 360.0f);
            componentToModify.pitch.Value = Mathf.Clamp(componentToModify.pitch - commands.mouseDeltaY * m_Sensitivity, -90.0f, 90.0f);
        }

        public override void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
            commandsToModify.mouseDeltaX.Value = InputProvider.GetMouseInput(MouseMovement.X);
            commandsToModify.mouseDeltaY.Value = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        protected override void SynchronizeBehavior(PlayerComponent componentToApply)
        {
            transform.rotation = Quaternion.AngleAxis(componentToApply.yaw, Vector3.up);
        }
    }
}