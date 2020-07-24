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
        protected override void TernaryUse(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            if (WithoutServerHit(session, playerId, m_EditDistance, out RaycastHit hit)) return;

            var designer = session.GetModifyingPayerFromId(playerId).Require<DesignerPlayerComponent>();
            var voxelInjector = (Injector) session.Injector;
            var position = (Position3Int) hit.point;
            voxelInjector.ApplyVoxelChange(position, new VoxelChange{id = designer.selectedVoxelId.AsNullable, magnitude = designer.editRadius});
        }

        protected override void RemoveBlock(SessionBase session, Injector injector, in Position3Int position)
        {
            if (ChunkManager.Singleton.GetMapSaveVoxel(position) is VoxelChange save)
                injector.ApplyVoxelChange(position, new VoxelChange {id = save.id, hasBlock = false, natural = true});
        }

        protected override void RemoveVoxelRadius(SessionBase session, int playerId, Injector injector, in Position3Int position)
        {
            float radius = session.GetModifyingPayerFromId(playerId).Require<DesignerPlayerComponent>().editRadius;
            if (radius > Mathf.Epsilon)
                injector.ApplyVoxelChange(position, new VoxelChange{magnitude = -radius});
        }
    }
}