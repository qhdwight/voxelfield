using Components;
using Input;
using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    public class PlayerCameraBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private float m_Sensitivity = 10.0f;

        public override void ModifyTrusted(Container containerToModify, Container commandsContainer, float duration)
        {
            if (!containerToModify.If(out CameraComponent cameraComponent) || !commandsContainer.If(out MouseComponent mouseComponent)) return;
            base.ModifyTrusted(containerToModify, commandsContainer, duration);
            cameraComponent.yaw.Value = Mathf.Repeat(cameraComponent.yaw + mouseComponent.mouseDeltaX * m_Sensitivity, 360.0f);
            cameraComponent.pitch.Value = Mathf.Clamp(cameraComponent.pitch - mouseComponent.mouseDeltaY * m_Sensitivity, -90.0f, 90.0f);
        }

        public override void ModifyCommands(Container commandsToModify)
        {
            if (!commandsToModify.If(out MouseComponent mouseComponent)) return;
            mouseComponent.mouseDeltaX.Value = InputProvider.GetMouseInput(MouseMovement.X);
            mouseComponent.mouseDeltaY.Value = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        protected override void SynchronizeBehavior(Container containersToApply)
        {
            if (!containersToApply.If(out CameraComponent cameraComponent)) return;
            if (cameraComponent.yaw.HasValue) transform.rotation = Quaternion.AngleAxis(cameraComponent.yaw, Vector3.up);
        }
    }
}