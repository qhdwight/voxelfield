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
        protected override bool CanTernaryUse(ItemComponent item, InventoryComponent inventory) => base.CanPrimaryUse(item, inventory);

        protected override void TernaryUse(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            if (WithoutServerHit(session, playerId, m_EditDistance, out RaycastHit hit)) return;

            var designer = session.GetPlayerFromId(playerId).Require<DesignerPlayerComponent>();
            var voxelInjector = (VoxelInjector) session.Injector;
            var position = (Position3Int) hit.point;
            voxelInjector.SetVoxelRadius(position, designer.editRadius, additiveChange: new VoxelChangeData {id = designer.selectedVoxelId.AsNullable}, isAdditive: true);
        }

        protected override void RemoveBlock(SessionBase session, VoxelInjector injector, in Position3Int position)
            => injector.SetVoxelData(position, new VoxelChangeData {id = ChunkManager.Singleton.GetMapSaveVoxel(position).Value.id, natural = true});

        protected override void SetVoxelRadius(SessionBase session, VoxelInjector injector, in Position3Int position)
            => injector.SetVoxelRadius(position, session.GetLocalCommands().Require<DesignerPlayerComponent>().editRadius);
    }
}