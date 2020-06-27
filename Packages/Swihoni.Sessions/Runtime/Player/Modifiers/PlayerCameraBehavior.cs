using Input;
using Swihoni.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerCameraBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private Transform m_MoveTransform = default;

        public override void ModifyTrusted(SessionBase session, int playerId, Container trustedPlayer, Container verifiedPlayer, Container commands, uint durationUs)
        {
            if (trustedPlayer.Without(out CameraComponent playerCamera)
             || commands.Without(out MouseComponent mouse)
             || trustedPlayer.WithPropertyWithValue(out HealthProperty health) && health.IsDead) return;
            base.ModifyTrusted(session, playerId, trustedPlayer, verifiedPlayer, commands, durationUs);

            var inventory = verifiedPlayer.Require<InventoryComponent>();
            bool isAds = inventory.HasItemEquipped && inventory.adsStatus.id == AdsStatusId.Ads;
            float multiplier = isAds ? ItemAssetLink.GetVisualPrefab(inventory.EquippedItemComponent.id).FovMultiplier : 1.0f;

            playerCamera.yaw.Value = Mathf.Repeat(playerCamera.yaw + mouse.mouseDeltaX * InputProvider.Singleton.Sensitivity * multiplier, 360.0f);
            playerCamera.pitch.Value = Mathf.Clamp(playerCamera.pitch - mouse.mouseDeltaY * InputProvider.Singleton.Sensitivity * multiplier, -90.0f, 90.0f);
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
            if (playerCamera.yaw.WithValue) m_MoveTransform.rotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up);
        }
    }
}