using Input;
using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerCameraBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private Transform m_MoveTransform = default;

        public override void ModifyTrusted(SessionBase session, int playerId, Container player, Container commands, float duration)
        {
            if (player.Without(out CameraComponent playerCamera)
             || commands.Without(out MouseComponent mouse)
             || player.Present(out HealthProperty health) && health.IsDead) return;
            base.ModifyTrusted(session, playerId, player, commands, duration);
            playerCamera.yaw.Value = Mathf.Repeat(playerCamera.yaw + mouse.mouseDeltaX * InputProvider.Singleton.Sensitivity, 360.0f);
            playerCamera.pitch.Value = Mathf.Clamp(playerCamera.pitch - mouse.mouseDeltaY * InputProvider.Singleton.Sensitivity, -90.0f, 90.0f);
        }

        public override void ModifyCommands(SessionBase session, Container commands)
        {
            if (commands.Without(out MouseComponent mouse)) return;
            mouse.mouseDeltaX.Value = InputProvider.GetMouseInput(MouseMovement.X);
            mouse.mouseDeltaY.Value = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        internal override void SynchronizeBehavior(Container player)
        {
            if (player.Without(out CameraComponent playerCamera)) return;
            if (playerCamera.yaw.HasValue) m_MoveTransform.rotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up);
        }
    }
}