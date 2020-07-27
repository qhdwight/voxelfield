using Swihoni.Sessions;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxelfield.Session;
using Voxels;

namespace Voxelfield.Item
{
    [CreateAssetMenu(fileName = "Super Pickaxe", menuName = "Item/Super Pickaxe", order = 0)]
    public class SuperPickaxe : SculptingItem
    {
        protected override void TernaryUse(in ModifyContext context, ItemComponent item)
        {
            if (WithoutServerHit(context, m_EditDistance, out RaycastHit hit)) return;

            var designer = context.player.Require<DesignerPlayerComponent>();
            var server = (ServerInjector) context.session.Injector;
            var position = (Position3Int) hit.point;
            VoxelChange change = context.player.Require<DesignerPlayerComponent>().selectedVoxel;
            change.Merge(new VoxelChange {magnitude = designer.editRadius, form = VoxelVolumeForm.Spherical});
            server.EvaluateVoxelChange(position, change);
        }

        protected override void PlaceBlock(in ModifyContext context, ServerInjector server, in Position3Int position)
        {
            VoxelChange change = context.player.Require<DesignerPlayerComponent>().selectedVoxel;
            change.Merge(new VoxelChange {hasBlock = true, form = VoxelVolumeForm.Single});
            server.EvaluateVoxelChange(position, change);
        }

        protected override bool OverrideBreakable => true;

        protected override void RemoveBlock(in ModifyContext context, ServerInjector server, in Position3Int position)
        {
            if (ChunkManager.Singleton.GetMapSaveVoxel(position) is VoxelChange save)
                server.EvaluateVoxelChange(position, save, overrideBreakable: true);
        }

        protected override void RemoveVoxelRadius(in ModifyContext context, ServerInjector server, in Position3Int position)
        {
            float radius = context.player.Require<DesignerPlayerComponent>().editRadius;
            if (radius > Mathf.Epsilon)
                server.EvaluateVoxelChange(position, new VoxelChange {magnitude = -radius, form = VoxelVolumeForm.Spherical});
        }
    }
}