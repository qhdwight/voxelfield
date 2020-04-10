using Components;
using Input;
using Session.Player.Components;
using UnityEngine;

namespace Session.Player.Modifiers
{
    public class PlayerCameraBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private float m_Sensitivity = 10.0f;

        public override void ModifyTrusted(ContainerBase containerToModify, ContainerBase commandsContainer, float duration)
        {
            if (!containerToModify.WithComponent(out CameraComponent cameraComponent) || !commandsContainer.WithComponent(out MouseComponent mouseComponent)) return;
            base.ModifyTrusted(containerToModify, commandsContainer, duration);
            cameraComponent.yaw.Value = Mathf.Repeat(cameraComponent.yaw + mouseComponent.mouseDeltaX * m_Sensitivity, 360.0f);
            cameraComponent.pitch.Value = Mathf.Clamp(cameraComponent.pitch - mouseComponent.mouseDeltaY * m_Sensitivity, -90.0f, 90.0f);
        }

        public override void ModifyCommands(ContainerBase commandsToModify)
        {
            if (!commandsToModify.WithComponent(out MouseComponent mouseComponent)) return;
            mouseComponent.mouseDeltaX.Value = InputProvider.GetMouseInput(MouseMovement.X);
            mouseComponent.mouseDeltaY.Value = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        protected override void SynchronizeBehavior(ContainerBase containersToApply)
        {
            if (!containersToApply.WithComponent(out CameraComponent cameraComponent)) return;
            transform.rotation = Quaternion.AngleAxis(cameraComponent.yaw, Vector3.up);
        }
    }
}