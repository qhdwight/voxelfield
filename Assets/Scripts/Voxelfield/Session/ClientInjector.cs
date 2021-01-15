using System;
using System.Collections.Generic;
using LiteNetLib.Utils;
using Steamworks;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;
using Voxelfield.Integration;
using Voxels;

namespace Voxelfield.Session
{
    public partial class ClientInjector : Injector
    {
        private readonly Pool<OrderedVoxelChangesProperty> m_ChangesPool = new Pool<OrderedVoxelChangesProperty>(1, () => new OrderedVoxelChangesProperty());
        private readonly UIntProperty m_Pointer = new UIntProperty();
        private readonly SortedDictionary<uint, OrderedVoxelChangesProperty> m_OrderedTickChanges = new SortedDictionary<uint, OrderedVoxelChangesProperty>();
        private readonly TouchedChunks m_TouchedChunks = new TouchedChunks();

        public override NetDataWriter GetConnectWriter()
        {
            var writer = new NetDataWriter();
            RequestConnectionComponent request = m_RequestConnection;
            request.version.SetTo(Application.version);
            if (TryGetSteamAuthTicket())
            {
                request.steamAuthenticationToken.SetTo(Convert.ToBase64String(m_SteamAuthenticationTicket.Data));
                request.steamPlayerId.Value = SteamClient.SteamId;
            }
            request.gameLiftPlayerSessionId.SetTo(GameLiftClientManager.PlayerSessionId);
            request.Serialize(writer);
            return writer;
        }

        protected override void OnClientReceive(ServerSessionContainer serverSession)
        {
            var serverChanges = serverSession.Require<OrderedVoxelChangesProperty>();
            UIntProperty serverTick = serverSession.Require<ServerStampComponent>().tick;

            if (serverChanges.Count > 0)
            {
                OrderedVoxelChangesProperty changes = m_ChangesPool.Obtain();
                changes.SetTo(serverChanges);
                m_OrderedTickChanges.Add(serverTick, changes);
            }

            HandleMapReload(serverSession);

            var tickSkipped = false;
            bool shouldApply = m_Pointer.WithoutValue || serverTick - m_Pointer == 1 || (tickSkipped = serverTick - m_Pointer > serverSession.Require<TickRateProperty>() * 3);
            if (!shouldApply) return;

            ApplyStoredChanges();
            if (tickSkipped) Debug.LogError($"Did not receive voxel changes for {m_Pointer.Value}");
            m_Pointer.Value = serverTick;
        }

        private void ApplyStoredChanges()
        {
            // ReSharper disable once ForeachCanBePartlyConvertedToQueryUsingAnotherGetEnumerator - Avoid allocation
            foreach (OrderedVoxelChangesProperty changes in m_OrderedTickChanges.Values)
            foreach (VoxelChange change in changes.List)
                ChunkManager.ApplyVoxelChanges(change, existingTouched: m_TouchedChunks);
            m_TouchedChunks.UpdateMesh();
            m_OrderedTickChanges.Clear();
            m_ChangesPool.ReturnAll();
        }
    }
}