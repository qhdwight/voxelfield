using Swihoni.Components;
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

        public float EditDistance => m_EditDistance;
        public LayerMask ChunkMask => m_ChunkMask;

        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            base.Swing(session, playerId, item, durationUs); // Melee damage
            if (WithoutServerHit(session, playerId, m_EditDistance, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel.Voxel voxel)) return;

            var voxelInjector = (VoxelInjector) session.Injector;
            switch (voxel.renderType)
            {
                case VoxelRenderType.Block:
                    RemoveBlock(session, voxelInjector, position);
                    break;
                case VoxelRenderType.Smooth:
                    SetVoxelRadius(session, playerId, voxelInjector, position);
                    break;
            }
            var brokeVoxelTickProperty = session.GetPlayerFromId(playerId).Require<BrokeVoxelTickProperty>();
            if (brokeVoxelTickProperty.WithValue) brokeVoxelTickProperty.Value++;
            else brokeVoxelTickProperty.Value = 0;
        }

        protected virtual void RemoveBlock(SessionBase session, VoxelInjector injector, in Position3Int position)
            => injector.SetVoxelData(position, new VoxelChangeData {renderType = VoxelRenderType.Smooth, natural = false});

        protected virtual void SetVoxelRadius(SessionBase session, int playerId, VoxelInjector injector, in Position3Int position)
            => injector.SetVoxelRadius(position, m_DestroyRadius, true);

        protected override bool CanSecondaryUse(ItemComponent item, InventoryComponent inventory) => base.CanPrimaryUse(item, inventory);

        protected static bool WithoutInnerVoxel(in RaycastHit hit, out Position3Int position, out Voxel.Voxel voxel)
        {
            // TODO:refactor similar to outer
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

        protected bool WithoutServerHit(SessionBase session, int playerId, float distance, out RaycastHit hit)
        {
            if (session.GetPlayerFromId(playerId).Without<ServerTag>())
            {
                hit = default;
                return true;
            }
            Ray ray = session.GetRayForPlayerId(playerId);
            return !Physics.Raycast(ray, out hit, distance, m_ChunkMask);
        }
        
        protected bool WithoutClientHit(SessionBase session, int playerId, float distance, out RaycastHit hit)
        {
            Container player = session.GetPlayerFromId(playerId);
            if (player.With<ServerTag>() && player.Without<HostTag>())
            {
                hit = default;
                return true;
            }
            Ray ray = session.GetRayForPlayerId(playerId);
            return !Physics.Raycast(ray, out hit, distance, m_ChunkMask);
        }

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            // TODO:feature add client side prediction for placing blocks
            if (WithoutServerHit(session, playerId, m_EditDistance, out RaycastHit hit)
             || WithoutOuterVoxel(hit, out Position3Int position, out Voxel.Voxel _)) return;

            var voxelInjector = (VoxelInjector) session.Injector;
            byte texture = session.GetPlayerFromId(playerId).With(out DesignerPlayerComponent designer) && designer.selectedVoxelId.WithValue
                ? designer.selectedVoxelId
                : VoxelId.Stone;
            voxelInjector.SetVoxelData(position, new VoxelChangeData {renderType = VoxelRenderType.Block, id = texture});
        }
    }
}