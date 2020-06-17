using Compound.Session;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;

namespace Compound.Item
{
    [CreateAssetMenu(fileName = "Sculpting", menuName = "Item/Sculpting", order = 0)]
    public class SculptingItem : MeleeModifier
    {
        [SerializeField] private float m_EditDistance = 5.0f, m_DestroyRadius = 1.7f;
        [SerializeField] private LayerMask m_ChunkMask = default;

        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            base.Swing(session, playerId, item, durationUs);
            if (session.GetPlayerFromId(playerId).Without<ServerTag>() || !GetVoxelRaycast(session, playerId, out RaycastHit hit)) return;
            var position = (Position3Int) (hit.point - hit.normal * 0.5f);
            Voxel.Voxel? voxel = ChunkManager.Singleton.GetVoxel(position);
            if (!voxel.HasValue) return;
            var voxelInjector = (VoxelInjector) session.Injector;
            switch (voxel.Value.renderType)
            {
                case VoxelRenderType.Block:
                    voxelInjector.SetVoxelData(position, new VoxelChangeData {renderType = VoxelRenderType.Smooth});
                    break;
                case VoxelRenderType.Smooth:
                    voxelInjector.RemoveVoxelRadius(position, m_DestroyRadius, true);
                    break;
            }
        }

        protected bool GetVoxelRaycast(SessionBase session, int playerId, out RaycastHit hit)
        {
            Ray ray = session.GetRayForPlayerId(playerId);
            return Physics.Raycast(ray, out hit, m_EditDistance, m_ChunkMask);
        }

        protected override bool HasSecondaryUse() => true;

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            // TODO:feature add client side prediction for placing blocks
            if (session.GetPlayerFromId(playerId).Without<ServerTag>()) return;
            if (!GetVoxelRaycast(session, playerId, out RaycastHit hit)) return;
            Vector3 position = hit.point + hit.normal * 0.5f;
            var voxelInjector = (VoxelInjector) session.Injector;
            voxelInjector.SetVoxelData((Position3Int) position, new VoxelChangeData {renderType = VoxelRenderType.Block, texture = VoxelTexture.Stone});
        }
    }
}