using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;
using Voxels;

namespace Voxelfield.Session.Mode
{
    [CreateAssetMenu(fileName = "Designer", menuName = "Session/Mode/Designer", order = 0)]
    public class DesignerMode : ModeBase
    {
        protected override void SpawnPlayer(in SessionContext context, bool begin = false)
        {
            Container player = context.player;
            if (player.With(out MoveComponent move))
            {
                move.Zero();
                move.position.Value = new Vector3 {y = 10.0f};
                move.type.Value = MoveType.Flying;
            }
            if (begin) player.ZeroIfWith<StatsComponent>();
            player.Require<ByteIdProperty>().Value = 1;
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health)) health.Value = ConfigManagerBase.Active.respawnHealth;
            player.ZeroIfWith<RespawnTimerProperty>();
            if (player.With(out InventoryComponent inventory))
            {
                PlayerItemManagerModiferBehavior.ResetEquipStatus(inventory);
                PlayerItemManagerModiferBehavior.SetAllItems(inventory, ItemId.SuperPickaxe, ItemId.VoxelWand, ItemId.ModelWand);
            }
            var designer = player.Require<DesignerPlayerComponent>();
            designer.NavigateProperties(_p =>
            {
                _p.Clear();
                _p.IsOverride = true;
            });
            designer.selectedVoxel.Value = new VoxelChange {texture = VoxelTexture.Checkered};
        }
    }
}