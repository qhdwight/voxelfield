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
        protected override void TertiaryUse(in SessionContext context, ItemComponent item)
        {
            if (WithoutHit(context, m_EditDistance, out RaycastHit hit) || !(context.session.Injector is ServerInjector server)) return;

            var designer = context.player.Require<DesignerPlayerComponent>();
            if (designer.editRadius < Mathf.Epsilon) return;

            var position = (Position3Int) hit.point;
            VoxelChange change = context.player.Require<DesignerPlayerComponent>().selectedVoxel;
            change.Merge(new VoxelChange {position = position, magnitude = designer.editRadius, form = VoxelVolumeForm.Spherical});
            server.ApplyVoxelChanges(change, overrideBreakable: true);
        }

        protected override void QuaternaryUse(in SessionContext context) => PickVoxel(context);

        protected override bool CanQuaternaryUse(in SessionContext context, ItemComponent item, InventoryComponent inventory) => base.CanPrimaryUse(item, inventory);

        protected override void PlaceBlock(in SessionContext context, ServerInjector server, in Position3Int position)
        {
            VoxelChange change = context.player.Require<DesignerPlayerComponent>().selectedVoxel;
            change.Merge(new VoxelChange {position = position, hasBlock = true, form = VoxelVolumeForm.Single});
            server.ApplyVoxelChanges(change);
        }

        protected override bool OverrideBreakable => true;

        protected override void RemoveBlock(in SessionContext context, ServerInjector server, in Position3Int position)
            => server.ApplyVoxelChanges(new VoxelChange {position = position, form = VoxelVolumeForm.Single, revert = true}, overrideBreakable: true);

        protected override void RemoveVoxelRadius(in SessionContext context, ServerInjector server, in Position3Int position)
        {
            float radius = context.player.Require<DesignerPlayerComponent>().editRadius;
            if (radius > Mathf.Epsilon)
                server.ApplyVoxelChanges(new VoxelChange {position = position, magnitude = -radius, form = VoxelVolumeForm.Spherical}, overrideBreakable: OverrideBreakable);
        }
    }
}