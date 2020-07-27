using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Math;
using UnityEngine;
using Voxelfield.Session;
using Voxels;

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

        protected override void Swing(in ModifyContext context, ItemComponent item)
        {
            base.Swing(context, item); // Melee damage
            if (WithoutServerHit(context, m_EditDistance, out RaycastHit hit)
             || WithoutInnerVoxel(hit, out Position3Int position, out Voxel voxel)) return;

            var server = (ServerInjector) context.session.Injector;
            if (voxel.HasBlock) RemoveBlock(context, server, position);
            else RemoveVoxelRadius(context, server, (Position3Int) (hit.point));
            // else RemoveVoxelRadius(context, server, position);
            var brokeVoxelTickProperty = context.player.Require<BrokeVoxelTickProperty>();
            if (brokeVoxelTickProperty.WithValue) brokeVoxelTickProperty.Value++;
            else brokeVoxelTickProperty.Value = 0;
        }

        protected virtual void RemoveBlock(in ModifyContext context, ServerInjector server, in Position3Int position)
            => server.EvaluateVoxelChange(position, new VoxelChange {hasBlock = false, natural = false, form = VoxelVolumeForm.Single});

        protected virtual void RemoveVoxelRadius(in ModifyContext context, ServerInjector server, in Position3Int position)
        {
            var change = new VoxelChange {magnitude = -m_DestroyRadius, replace = true, color = Voxel.Dirt, texture = VoxelTexture.Checkered, form = VoxelVolumeForm.Spherical};
            server.EvaluateVoxelChange(position, change);
        }

        protected bool WithoutInnerVoxel(in RaycastHit hit, out Position3Int position, out Voxel voxel)
        {
            // TODO:refactor similar to outer
            position = (Position3Int) (hit.point - hit.normal * 0.1f);
            Voxel? optionalVoxel = ChunkManager.Singleton.GetVoxel(position);
            voxel = optionalVoxel ?? default;
            return !optionalVoxel.HasValue || !voxel.IsBreakable && !OverrideBreakable;
        }

        protected bool WithoutOuterVoxel(in RaycastHit hit, out Position3Int position, out Voxel voxel)
        {
            position = (Position3Int) (hit.point + hit.normal * 0.1f);
            Voxel? _voxel = ChunkManager.Singleton.GetVoxel(position);
            voxel = _voxel ?? default;
            return !_voxel.HasValue || !voxel.IsBreakable && !OverrideBreakable;
        }

        protected bool WithoutServerHit(in ModifyContext context, float distance, out RaycastHit hit)
        {
            if (context.player.Without<ServerTag>())
            {
                hit = default;
                return true;
            }
            Ray ray = context.session.GetRayForPlayerId(context.playerId);
            return !Physics.Raycast(ray, out hit, distance, m_ChunkMask);
        }

        protected bool WithoutClientHit(in ModifyContext context, float distance, out RaycastHit hit)
        {
            Container player = context.player;
            if (player.With<ServerTag>() && player.Without<HostTag>())
            {
                hit = default;
                return true;
            }
            Ray ray = context.session.GetRayForPlayerId(context.playerId);
            return !Physics.Raycast(ray, out hit, distance, m_ChunkMask);
        }

        private Position3Int? m_CachedPosition; // Guaranteed set by can use and tested in actual use 

        protected override bool CanSecondaryUse(in ModifyContext context, ItemComponent item, InventoryComponent inventory)
        {
            if (!base.CanPrimaryUse(item, inventory) || WithoutServerHit(context, m_EditDistance, out RaycastHit hit)
                                                     || WithoutOuterVoxel(hit, out Position3Int position, out Voxel _)
                                                     || NoSolution(context.player, position))
            {
                m_CachedPosition = null;
                return false;
            }
            m_CachedPosition = position;
            return true;
            // return AdjustPosition(m_CachedPosition.Value, session.GetModifyingPayerFromId(playerId));
        }

        protected virtual bool OverrideBreakable => false;

        protected override void SecondaryUse(in ModifyContext context)
        {
            // TODO:feature add client side prediction for placing blocks
            if (!(m_CachedPosition is Position3Int position)) return;

            var server = (ServerInjector) context.session.Injector;
            PlaceBlock(context, server, position);
        }

        protected virtual void PlaceBlock(in ModifyContext context, ServerInjector server, in Position3Int position)
            => server.EvaluateVoxelChange(position, new VoxelChange {hasBlock = true, texture = VoxelTexture.Checkered, color = Voxel.Dirt, form = VoxelVolumeForm.Single});

        private bool NoSolution(Container player, in Position3Int position, float radius = 0.9f)
        {
            if (!m_PreventPlacingOnSelf) return false;

            var move = player.Require<MoveComponent>();
            Vector3 eyePosition = move.GetPlayerEyePosition();
            return ExtraMath.SquareDistance(eyePosition, position) < radius * radius;

            // var playerCollider = session.GetPlayerModifier(player, playerId).GetComponentInChildren<PlayerTrigger>().GetComponent<Collider>();
            // Chunk chunk = ChunkManager.Singleton.GetChunkFromWorldPosition((Position3Int) playerCollider.transform.position);
            // Collider chunkCollider = chunk.MeshCollider;
            // if (Physics.ComputePenetration(playerCollider, playerCollider.transform.position, playerCollider.transform.rotation,
            //                                chunkCollider, chunkCollider.transform.position, chunkCollider.transform.rotation,
            //                                out Vector3 direction, out float distance))
            // {
            //     var move = player.Require<MoveComponent>();
            //     move.position.Value += direction * distance;
            //     Debug.Log(direction * distance, chunk);
            // }
            // ChunkManager.Singleton.GetVoxel((Position3Int) eyePosition)
            // Voxel.Voxel? _voxel = ChunkManager.Singleton.GetVoxel((Position3Int) eyePosition);
            // return _voxel is Voxel.Voxel voxel && voxel.HasBlock;
            // float distance = position.y - move.position.Value.y,
            //       absoluteDistance = Mathf.Abs(distance);
            // if (absoluteDistance > radius)
            // {
            //     if (distance > 0.0f)
            //     {
            //         move.position.Value += new Vector3 {y = absoluteDistance};
            //     }
            //     else
            //     {
            //     }
            // }
            // return position != playerPosition && position != playerPosition + new Position3Int {y = 1};

            // if (voxel.HasBlock) return true;
            // float height = Mathf.Lerp(1.26f, 1.8f, 1.0f - move.normalizedCrouch);
            // if (Physics.Raycast(eyePosition, Vector3.down, out RaycastHit hit, height + 0.05f, ChunkMask))
            //     move.position.Value += new Vector3 {y = height - hit.distance};
        }

        protected override bool CanTernaryUse(in ModifyContext context, ItemComponent item, InventoryComponent inventory)
        {
            if (!base.CanPrimaryUse(item, inventory) || WithoutServerHit(context, m_EditDistance, out RaycastHit hit)
                                                     || NoSolution(context.player, (Position3Int) hit.point, m_AdditiveRadius))
            {
                m_CachedPosition = null;
                return false;
            }
            m_CachedPosition = (Position3Int) (hit.point + hit.normal);
            return true;
        }

        protected override void TernaryUse(in ModifyContext context, ItemComponent item)
        {
            if (!(m_CachedPosition is Position3Int position)) return;

            var server = (ServerInjector) context.session.Injector;
            var change = new VoxelChange {texture = VoxelTexture.Checkered, color = Voxel.Dirt, magnitude = m_AdditiveRadius, form = VoxelVolumeForm.Spherical};
            server.EvaluateVoxelChange(position, change);
        }
    }
}