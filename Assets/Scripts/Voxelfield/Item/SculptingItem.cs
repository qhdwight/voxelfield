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
        [SerializeField] protected float m_AdditiveRadius = 1.5f;
        [SerializeField] private LayerMask m_ChunkMask = default;
        [SerializeField] private bool m_PreventPlacingOnSelf = true;

        public float EditDistance => m_EditDistance;
        public LayerMask ChunkMask => m_ChunkMask;

        protected override void Swing(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            base.Swing(session, playerId, item, durationUs); // Melee damage
            if (WithoutServerHit(session, playerId, m_EditDistance, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel.Voxel voxel)) return;

            var voxelInjector = (Injector) session.Injector;
            if (voxel.HasBlock) RemoveBlock(session, voxelInjector, position);
            else RemoveVoxelRadius(session, playerId, voxelInjector, position);
            var brokeVoxelTickProperty = session.GetModifyingPayerFromId(playerId).Require<BrokeVoxelTickProperty>();
            if (brokeVoxelTickProperty.WithValue) brokeVoxelTickProperty.Value++;
            else brokeVoxelTickProperty.Value = 0;
        }

        protected virtual void RemoveBlock(SessionBase session, Injector injector, in Position3Int position)
            => injector.SetVoxelData(position, new VoxelChangeData {hasBlock = false, natural = false});

        protected virtual void RemoveVoxelRadius(SessionBase session, int playerId, Injector injector, in Position3Int position)
            => injector.SetVoxelRadius(position, m_DestroyRadius, true);

        protected static bool WithoutInnerVoxel(in RaycastHit hit, out Position3Int position, out Voxel.Voxel voxel)
        {
            // TODO:refactor similar to outer
            position = (Position3Int) (hit.point - hit.normal * 0.1f);
            Voxel.Voxel? optionalVoxel = ChunkManager.Singleton.GetVoxel(position);
            voxel = optionalVoxel ?? default;
            return !optionalVoxel.HasValue || !voxel.IsBreakable;
        }

        protected static bool WithoutOuterVoxel(in RaycastHit hit, out Position3Int position, out Voxel.Voxel voxel)
        {
            position = (Position3Int) (hit.point + hit.normal * 0.1f);
            Voxel.Voxel? optionalVoxel = ChunkManager.Singleton.GetVoxel(position);
            voxel = optionalVoxel ?? default;
            return !optionalVoxel.HasValue || !voxel.IsBreakable;
        }

        protected bool WithoutServerHit(SessionBase session, int playerId, float distance, out RaycastHit hit)
        {
            if (session.GetModifyingPayerFromId(playerId).Without<ServerTag>())
            {
                hit = default;
                return true;
            }
            Ray ray = session.GetRayForPlayerId(playerId);
            return !Physics.Raycast(ray, out hit, distance, m_ChunkMask);
        }

        protected bool WithoutClientHit(SessionBase session, int playerId, float distance, out RaycastHit hit)
        {
            Container player = session.GetModifyingPayerFromId(playerId);
            if (player.With<ServerTag>() && player.Without<HostTag>())
            {
                hit = default;
                return true;
            }
            Ray ray = session.GetRayForPlayerId(playerId);
            return !Physics.Raycast(ray, out hit, distance, m_ChunkMask);
        }

        private Position3Int? m_CachedPosition; // Guaranteed set by can use and tested in actual use 

        protected override bool CanSecondaryUse(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory)
        {
            if (!base.CanPrimaryUse(item, inventory) || WithoutServerHit(session, playerId, m_EditDistance, out RaycastHit hit)
                                                     || WithoutOuterVoxel(hit, out Position3Int position, out Voxel.Voxel _))
            {
                m_CachedPosition = null;
                return false;
            }
            m_CachedPosition = position;
            return CanPlace(m_CachedPosition.Value, session.GetModifyingPayerFromId(playerId));
        }

        protected override void SecondaryUse(SessionBase session, int playerId, uint durationUs)
        {
            // TODO:feature add client side prediction for placing blocks
            if (!(m_CachedPosition is Position3Int position)) return;

            var voxelInjector = (Injector) session.Injector;
            Container player = session.GetModifyingPayerFromId(playerId);
            byte texture = player.With(out DesignerPlayerComponent designer) && designer.selectedVoxelId.WithValue
                ? designer.selectedVoxelId
                : VoxelId.Dirt;
            voxelInjector.SetVoxelData(position, new VoxelChangeData {hasBlock = true, id = texture});
        }

        private bool CanPlace(in Position3Int position, Container player)
        {
            if (!m_PreventPlacingOnSelf) return true;
            var playerPosition = (Position3Int) player.Require<MoveComponent>().position.Value;
            return position != playerPosition && position != playerPosition + new Position3Int {y = 1};
        }

        protected override bool CanTernaryUse(SessionBase session, int playerId, ItemComponent item, InventoryComponent inventory)
        {
            if (!base.CanPrimaryUse(item, inventory) || WithoutServerHit(session, playerId, m_EditDistance, out RaycastHit hit))
            {
                m_CachedPosition = null;
                return false;
            }
            m_CachedPosition = (Position3Int) hit.point;
            return CanPlace(m_CachedPosition.Value, session.GetModifyingPayerFromId(playerId));
        }

        protected override void TernaryUse(SessionBase session, int playerId, ItemComponent item, uint durationUs)
        {
            if (!(m_CachedPosition is Position3Int position)) return;

            var voxelInjector = (Injector) session.Injector;
            voxelInjector.SetVoxelRadius(position, m_AdditiveRadius, additiveChange: new VoxelChangeData {id = VoxelId.Dirt}, isAdditive: true);
        }
    }
}