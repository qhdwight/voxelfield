using Swihoni.Sessions;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxelfield.Session;

namespace Voxelfield.Item
{
    [CreateAssetMenu(fileName = "Super Pickaxe", menuName = "Item/Super Pickaxe", order = 0)]
    public class SuperPickaxe : SculptingItem
    {
        protected override void TernaryUse(in ModifyContext context, ItemComponent item)
        {
            if (WithoutServerHit(context, m_EditDistance, out RaycastHit hit)) return;

            var designer = context.player.Require<DesignerPlayerComponent>();
            var voxelInjector = (Injector) context.session.Injector;
            var position = (Position3Int) hit.point;
            voxelInjector.EvaluateVoxelChange(position, new VoxelChange{id = designer.selectedVoxelId.AsNullable, magnitude = designer.editRadius, form = VoxelVolumeForm.Sperhical});
        }

        protected override void RemoveBlock(in ModifyContext context, Injector injector, in Position3Int position)
        {
            if (ChunkManager.Singleton.GetMapSaveVoxel(position) is VoxelChange save)
                injector.EvaluateVoxelChange(position, new VoxelChange {id = save.id, hasBlock = false, natural = true});
        }

        protected override void RemoveVoxelRadius(in ModifyContext context, Injector injector, in Position3Int position)
        {
            float radius = context.player.Require<DesignerPlayerComponent>().editRadius;
            if (radius > Mathf.Epsilon)
                injector.EvaluateVoxelChange(position, new VoxelChange{magnitude = -radius, form = VoxelVolumeForm.Sperhical});
        }
    }
}