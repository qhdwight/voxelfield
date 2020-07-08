using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxelfield.Session;

namespace Voxelfield.Item
{
    [CreateAssetMenu(fileName = "Sculpting", menuName = "Item/Sculpting", order = 0)]
    public class SculptingItem : MeleeModifier
    {
        [SerializeField] protected float m_EditDistance = 5.0f;
        [SerializeField] protected float m_DestroyRadius = 1.7f;
        [SerializeField] private LayerMask m_ChunkMask = default;

        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            base.Swing(session, playerId, item, durationUs); // Melee damage
            if (WithoutServerHit(session, playerId, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel.Voxel voxel)) return;

            var voxelInjector = (VoxelInjector) session.Injector;
            switch (voxel.renderType)
            {
                case VoxelRenderType.Block:
                    voxelInjector.SetVoxelData(position, new VoxelChangeData {renderType = VoxelRenderType.Smooth});
                    break;
                case VoxelRenderType.Smooth:
                    voxelInjector.SetVoxelRadius(position, m_DestroyRadius, true);
                    break;
            }
            var brokeVoxelTickProperty = session.GetPlayerFromId(playerId).Require<BrokeVoxelTickProperty>();
            if (brokeVoxelTickProperty.WithValue) brokeVoxelTickProperty.Value++;
            else brokeVoxelTickProperty.Value = 0;
        }

        protected override bool CanSecondaryUse(ItemComponent item, InventoryComponent inventory) => base.CanPrimaryUse(item, inventory);

        protected static bool WithoutInnerVoxel(in RaycastHit hit, out Position3Int position, out Voxel.Voxel voxel)
        {
            position = (Position3Int) (hit.point - hit.normal * 0.5f);
            Voxel.Voxel? optionalVoxel = ChunkManager.Singleton.GetVoxel(position);
            voxel = optionalVoxel ?? default;
            return !optionalVoxel.HasValue || !voxel.breakable;
        }
        
        protected static bool WithoutOuterVoxel(in RaycastHit hit, out Position3Int position, out Voxel.Voxel voxel)
        {
            position = (Position3Int) (hit.point + hit.normal * 0.5f);
            Voxel.Voxel? optionalVoxel = ChunkManager.Singleton.GetVoxel(position);
            voxel = optionalVoxel ?? default;
            return !optionalVoxel.HasValue || !voxel.breakable;
        }

        protected bool WithoutServerHit(SessionBase session, int playerId, out RaycastHit hit)
        {
            if (session.GetPlayerFromId(playerId).Without<ServerTag>())
            {
                hit = default;
                return true;
            }
            Ray ray = session.GetRayForPlayerId(playerId);
            return !Physics.Raycast(ray, out hit, m_EditDistance, m_ChunkMask);
        }

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            // TODO:feature add client side prediction for placing blocks
            if (WithoutServerHit(session, playerId, out RaycastHit hit)
             || WithoutOuterVoxel(hit, out Position3Int position, out Voxel.Voxel _)) return;
            
            var voxelInjector = (VoxelInjector) session.Injector;
            byte texture = session.GetPlayerFromId(playerId).With(out DesignerPlayerComponent designer) && designer.selectedBlockId.WithValue
                ? designer.selectedBlockId
                : VoxelId.Stone;
            voxelInjector.SetVoxelData(position, new VoxelChangeData {renderType = VoxelRenderType.Block, texture = texture});
        }
    }
}