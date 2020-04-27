using Input;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerCameraBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private float m_Sensitivity = 10.0f;
        [SerializeField] private Transform m_MoveTransform = default;

        public override void ModifyTrusted(SessionBase session, int playerId, Container player, Container commands, float duration)
        {
            if (player.Without(out CameraComponent cameraComponent)
             || commands.Without(out MouseComponent mouseComponent)
             || player.Present(out HealthProperty healthProperty) && healthProperty.IsDead) return;
            base.ModifyTrusted(session, playerId, player, commands, duration);
            cameraComponent.yaw.Value = Mathf.Repeat(cameraComponent.yaw + mouseComponent.mouseDeltaX * m_Sensitivity, 360.0f);
            cameraComponent.pitch.Value = Mathf.Clamp(cameraComponent.pitch - mouseComponent.mouseDeltaY * m_Sensitivity, -90.0f, 90.0f);
        }

        public override void ModifyCommands(SessionBase session, Container commands)
        {
            if (commands.Without(out MouseComponent mouseComponent)) return;
            mouseComponent.mouseDeltaX.Value = InputProvider.GetMouseInput(MouseMovement.X);
            mouseComponent.mouseDeltaY.Value = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        protected override void SynchronizeBehavior(Container player)
        {
            if (player.Without(out CameraComponent cameraComponent)) return;
            if (cameraComponent.yaw.HasValue) m_MoveTransform.rotation = Quaternion.AngleAxis(cameraComponent.yaw, Vector3.up);
        }
    }
}