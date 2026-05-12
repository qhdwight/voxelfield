using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Voxels;

namespace Voxelfield.Session
{
    public class ServerInjector : Injector
    {
        private const int OutsideBoundDamage = 75;

        private readonly OrderedVoxelChangesProperty m_MasterChanges = new();

        public void ApplyVoxelChanges(VoxelChange change, TouchedChunks touchedChunks = null, bool overrideBreakable = false)
        {
            void Apply() => m_MapManager.ChunkManager.ApplyVoxelChanges(change, true, touchedChunks, overrideBreakable);
            if (change.isUndo)
            {
                if (m_MasterChanges.TryRemoveEnd(out VoxelChange lastChange))
                {
                    change.undo = lastChange.undo;
                    Apply();
                }
            }
            else
            {
                if (!change.position.HasValue) throw new ArgumentException("Voxel change does not have position!");

                var changed = Session.GetLatestSession().Require<OrderedVoxelChangesProperty>();
                changed.Append(change);
                change.undo = new List<(Chunk, Position3Int, Voxel)>((int) change.magnitude.GetValueOrDefault(1).Square());
                m_MasterChanges.Append(change);
                Apply();
            }
        }

        public override void OnThrowablePopped(ThrowableModifierBehavior throwableBehavior)
        {
            var center = (Position3Int) throwableBehavior.transform.position;
            var change = new VoxelChange {position = center, magnitude = throwableBehavior.Radius * -0.4f, replace = true, modifiesBlocks = true, form = VoxelVolumeForm.Spherical};
            ApplyVoxelChanges(change);
        }

        protected override void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession)
        {
            var changedVoxels = sendSession.Require<OrderedVoxelChangesProperty>();
            changedVoxels.SetTo(m_MasterChanges);
        }

        protected override void OnServerNewConnection(ConnectionRequest socketRequest)
        {
            RequestConnectionComponent request = m_RequestConnection;
            void Reject(string message)
            {
                try
                {
                    m_RejectionWriter.Reset();
                    m_RejectionWriter.Put(message);
                    socketRequest.Reject(m_RejectionWriter);
                }
                catch (Exception exception)
                {
                    Debug.LogError($"Exception rejecting graciously: {exception.Message}");
                    socketRequest.RejectForce();
                }
            }
            try
            {
                int nextPeerId = ((NetworkedSessionBase) Session).Socket.NetworkManager.PeekNextPeerId();
                if (nextPeerId >= SessionBase.MaxPlayers - 1)
                {
                    Reject("Too many players are already connected to the server!");
                    return;
                }

                request.Deserialize(socketRequest.Data);
                string version = request.version.AsNewString();
                if (version != Application.version)
                {
                    Reject("Your version does not match that of the server.");
                    return;
                }

                socketRequest.Accept();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception on new connection: {exception}");
                Reject("Internal server error");
            }
        }
        
        protected override void OnMapChange() => m_MasterChanges.Zero();

        public override string GetUsername(in SessionContext context) => null;

        private static readonly RaycastHit[] CachedHits = new RaycastHit[2];

        private static void Suffocate(in SessionContext context, byte damage = 1)
        {
            var damageContext = new DamageContext(context, context.playerId, context.player, damage, "Suffocation");
            context.ModifyingMode.InflictDamage(damageContext);
        }

        public override void OnServerMove(in SessionContext context, MoveComponent move)
        {
            if (context.player.Health().IsDead) return;

            if (move.position.WithoutValue || move.type == MoveType.Flying) return;

            /* If the normal of the contact (of the upwards raycast) is aligned with the upward vector, it is a backface and we are in the terrain */
            Physics.queriesHitBackfaces = true;
            {
                for (var f = 0.1f; f < move.GetPlayerHeight(); f++)
                {
                    Vector3 origin = move + new Vector3 {y = f};
                    int count = context.PhysicsScene.Raycast(origin, Vector3.up, CachedHits, float.PositiveInfinity, 1 << 15);
                    if (CachedHits.TryClosest(count, out RaycastHit hit) && hit.normal.y > Mathf.Epsilon)
                    {
                        float distance = hit.distance;
                        move.position.Value += new Vector3 {y = distance + 0.05f};
                        if (f > 1.0f && m_MapManager.ChunkManager.GetVoxel((Position3Int) origin) is {IsBreathable: false})
                            Suffocate(context, OutsideBoundDamage);
                        break;
                    }
                }
            }
            Physics.queriesHitBackfaces = false;

            if (m_MapManager.Map.terrainGeneration.upperBreakableHeight.TryWithValue(out int upperLimit)
             && context.sessionContainer.Require<ModeIdProperty>() == ModeIdProperty.SecureArea
             && context.sessionContainer.Require<SecureAreaComponent>().roundTime.WithValue
             && move.position.Value.y > upperLimit + 5) Suffocate(context);

            // Vector3 normal = context.session.GetPlayerModifier(context.player, context.playerId).Movement.Hit.
            // Debug.Log(normal);
            // if (Vector3.Dot(normal, Vector3.down) > 0.0f)
            // {

            // }

            // Voxel.Voxel? _voxel = ChunkManager.Singleton.GetVoxel((Position3Int) eyePosition);
            // if (!(_voxel is Voxel.Voxel voxel) || voxel.OnlySmooth && voxel.density <= 255 / 2) return;
            //
            // var damageContext = new DamageContext(context, context.playerId, context.player, 1, "Suffocation");
            // context.session.GetModifyingMode(context.sessionContainer).InflictDamage(damageContext);
        }

        public override void OnPostTick(Container session) => HandleMapReload(session);

        public override void ModifyPlayer(in SessionContext context)
        {
            if (!context.WithServerStringCommands(out IEnumerable<string[]> commands)) return;
            foreach (string[] arguments in commands)
                switch (arguments.First())
                {
                    case "reload_map":
                        context.sessionContainer.Require<MapGenerationProperty>().Reload();
                        break;
                }
        }
    }
}