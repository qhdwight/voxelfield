using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;
using Voxel;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Designer", menuName = "Session/Mode/Designer", order = 0)]
    public class DesignerMode : ModeBase
    {
        protected override void SpawnPlayer(SessionBase session, Container player)
        {
            Debug.Log("Spawn Player");
            // TODO:refactor zeroing
            if (player.With(out MoveComponent move))
            {
                move.Zero();
                move.position.Value = new Vector3 {y = 10.0f};
                move.type.Value = MoveType.Flying;
            }
            player.Require<IdProperty>().Value = 1;
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health)) health.Value = 100;
            player.ZeroIfWith<RespawnTimerProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            if (player.With(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.AddItem(inventory, ItemId.Shovel);
                PlayerItemManagerModiferBehavior.AddItem(inventory, ItemId.VoxelWand);
                PlayerItemManagerModiferBehavior.AddItem(inventory, ItemId.ModelWand);
            }
        }

        public override void SetupNewPlayer(SessionBase session, Container player)
        {
            SpawnPlayer(session, player);
            var designer = player.Require<DesignerPlayerComponent>();
            designer.Reset();
            designer.selectedBlockId.Value = VoxelId.Stone;
        }
    }
}