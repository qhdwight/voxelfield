using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
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

        protected override void Swing(in SessionContext context, ItemComponent item)
        {
            base.Swing(context, item); // Melee damage
            if (!(context.session.Injector is ServerInjector server) || WithoutHit(context, m_EditDistance, out RaycastHit hit)) return;

            var position = (Position3Int) (hit.point - hit.normal * 0.1f);
            if (WithoutBreakableVoxel(position, out Voxel voxel)) return;

            if (voxel.HasBlock) RemoveBlock(context, server, position);
            else RemoveVoxelRadius(context, server, position);
            // else RemoveVoxelRadius(context, server, position);
            var brokeVoxelTickProperty = context.player.Require<BrokeVoxelTickProperty>();
            if (brokeVoxelTickProperty.WithValue) brokeVoxelTickProperty.Value++;
            else brokeVoxelTickProperty.Value = 0;
        }

        protected virtual void RemoveBlock(in SessionContext context, ServerInjector server, in Position3Int position)
            => server.ApplyVoxelChanges(new VoxelChange {position = position, form = VoxelVolumeForm.Single, hasBlock = false, natural = false});

        protected virtual void RemoveVoxelRadius(in SessionContext context, ServerInjector server, in Position3Int position)
        {
            var change = new VoxelChange
            {
                position = position, form = VoxelVolumeForm.Spherical,
                magnitude = -m_DestroyRadius, color = Voxel.Dirt, texture = VoxelTexture.Checkered
            };
            server.ApplyVoxelChanges(change);
        }

        public bool WithoutBreakableVoxel(in Position3Int position, out Voxel voxel)
        {
            Voxel? _voxel = ChunkManager.Singleton.GetVoxel(position);
            voxel = _voxel ?? default;
            return !_voxel.HasValue || !voxel.IsBreakable && !OverrideBreakable;
        }

        protected bool WithoutHit(in SessionContext context, float distance, out RaycastHit hit)
        {
            Ray ray = context.session.GetRayForPlayerId(context.playerId);
            int count = Physics.RaycastNonAlloc(ray, RaycastHits, distance, m_ChunkMask);
            return !RaycastHits.TryClosest(count, out hit);
        }

        public bool WithoutClientHit(in SessionContext context, float distance, out RaycastHit hit)
        {
            Container player = context.player;
            if (player.With<ServerTag>() && player.Without<HostTag>())
            {
                hit = default;
                return true;
            }
            Ray ray = context.session.GetRayForPlayerId(context.playerId);
            int count = Physics.RaycastNonAlloc(ray, RaycastHits, distance, m_ChunkMask);
            return !RaycastHits.TryClosest(count, out hit);
        }

        protected void PickVoxel(in SessionContext context)
        {
            if (WithoutClientHit(context, m_EditDistance, out RaycastHit hit)) return;

            var position = (Position3Int) (hit.point - hit.normal * 0.1f);
            if (WithoutBreakableVoxel(position, out Voxel voxel)) return;

            var designer = context.session.GetLocalCommands().Require<DesignerPlayerComponent>();
            VoxelChange pickedChange = default;
            pickedChange.color = voxel.color;
            pickedChange.texture = voxel.texture;
            designer.selectedVoxel.Value = pickedChange;
        }

        private Position3Int? m_CachedPosition; // Guaranteed set by can use and tested in actual use 

        protected override bool CanSecondaryUse(in SessionContext context, ItemComponent item, InventoryComponent inventory)
        {
            if (!base.CanPrimaryUse(item, inventory) || WithoutHit(context, m_EditDistance, out RaycastHit hit))
            {
                m_CachedPosition = null;
                return false;
            }
            var position = (Position3Int) (hit.point + hit.normal * 0.1f);
            if (WithoutBreakableVoxel(position, out Voxel _) || NoSolution(context.player, position))
            {
                m_CachedPosition = null;
                return false;
            }
            m_CachedPosition = position;
            return true;
        }

        protected virtual bool OverrideBreakable => false;

        protected override void SecondaryUse(in SessionContext context)
        {
            // TODO:feature add client side prediction for placing blocks
            if (!(m_CachedPosition is Position3Int position) || !(context.session.Injector is ServerInjector server)) return;

            PlaceBlock(context, server, position);
        }

        protected virtual void PlaceBlock(in SessionContext context, ServerInjector server, in Position3Int position)
            => server.ApplyVoxelChanges(new VoxelChange
            {
                position = position, form = VoxelVolumeForm.Single,
                hasBlock = true, texture = VoxelTexture.Checkered, color = Voxel.Dirt
            });

        private bool NoSolution(Container player, in Position3Int position, float radius = 0.9f)
        {
            return false && m_PreventPlacingOnSelf;
            // if (!m_PreventPlacingOnSelf) return false;
            //
            // var move = player.Require<MoveComponent>();
            // Vector3 eyePosition = move.GetPlayerEyePosition();
            // return ExtraMath.SquareDistance(eyePosition, position) < radius * radius;

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

        protected override bool CanTertiaryUse(in SessionContext context, ItemComponent item, InventoryComponent inventory)
        {
            if (!base.CanPrimaryUse(item, inventory) || WithoutHit(context, m_EditDistance, out RaycastHit hit))
            {
                m_CachedPosition = null;
                return false;
            }
            var position = (Position3Int) (hit.point + new Vector3(0.5f, 0.5f, 0.5f));
            if (NoSolution(context.player, position, m_AdditiveRadius))
            {
                m_CachedPosition = null;
                return false;
            }
            m_CachedPosition = position;
            return true;
        }

        protected override void TertiaryUse(in SessionContext context, ItemComponent item)
        {
            if (!(m_CachedPosition is Position3Int position) || !(context.session.Injector is ServerInjector server)) return;

            var change = new VoxelChange
            {
                position = position, form = VoxelVolumeForm.Spherical,
                texture = VoxelTexture.Checkered, color = Voxel.Dirt, magnitude = m_AdditiveRadius
            };
            server.ApplyVoxelChanges(change);
        }
    }
}