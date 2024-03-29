using Swihoni.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerCameraBehavior : PlayerModifierBehaviorBase
    {
        [SerializeField] private Transform m_MoveTransform = default;

        public override void ModifyTrusted(in SessionContext context, Container verifiedPlayer)
        {
            if (context.player.Without(out CameraComponent playerCamera)
             || context.commands.Without(out MouseComponent mouse)) return;
            base.ModifyTrusted(in context, verifiedPlayer);

            var inventory = verifiedPlayer.Require<InventoryComponent>();
            bool isAds = inventory.HasItemEquipped && inventory.adsStatus.id == AdsStatusId.Ads;
            float multiplier = isAds ? ItemAssetLink.GetVisualPrefab(inventory.EquippedItemComponent.id).FovMultiplier * DefaultConfig.Active.adsMultiplier : 1.0f;

            playerCamera.yaw.Value = Mathf.Repeat(playerCamera.yaw + mouse.mouseDeltaX * DefaultConfig.Active.sensitivity * multiplier, 360.0f);
            playerCamera.pitch.Value = Mathf.Clamp(playerCamera.pitch - mouse.mouseDeltaY * DefaultConfig.Active.sensitivity * multiplier, -90.0f, 90.0f);
        }

        public override void ModifyCommands(SessionBase session, Container commands, int playerId)
        {
            if (commands.Without(out MouseComponent mouse)) return;
            mouse.mouseDeltaX.Value = InputProvider.GetMouseInput(MouseMovement.X);
            mouse.mouseDeltaY.Value = InputProvider.GetMouseInput(MouseMovement.Y);
        }

        protected internal override void SynchronizeBehavior(in SessionContext context)
        {
            if (context.player.Without(out CameraComponent playerCamera)) return;
            if (playerCamera.yaw.WithValue) m_MoveTransform.rotation = Quaternion.AngleAxis(playerCamera.yaw, Vector3.up);
        }
    }
}