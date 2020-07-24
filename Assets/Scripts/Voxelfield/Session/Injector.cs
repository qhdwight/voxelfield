#if UNITY_EDITOR
#define VOXELFIELD_RELEASE_SERVER
#endif

using System;
using LiteNetLib.Utils;
using Steamworks;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Entities;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxel.Map;

namespace Voxelfield.Session
{
    [Serializable]
    public class RequestConnectionComponent : ComponentBase
    {
        public StringProperty version;
        public GameLiftPlayerSessionIdProperty gameLiftPlayerSessionId;
        public SteamAuthenticationTicketProperty steamAuthenticationToken;
        public SteamIdProperty steamPlayerId;
    }

    public class Injector : SessionInjectorBase
    {
        protected readonly RequestConnectionComponent m_RequestConnection = new RequestConnectionComponent();

        protected internal virtual void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true)
            => ChunkManager.Singleton.SetVoxelData(worldPosition, change, chunk, updateMesh);

        protected internal virtual void VoxelTransaction(VoxelChangeTransaction uncommitted) => uncommitted.Commit();

        protected internal virtual void SetVoxelRadius(in Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false,
                                                       bool destroyBlocks = false, bool isAdditive = false, in VoxelChangeData additiveChange = default,
                                                       ChangedVoxelsProperty changedVoxels = null)
            => ChunkManager.Singleton.SetVoxelRadius(worldPosition, radius, replaceGrassWithDirt, destroyBlocks, isAdditive, additiveChange, changedVoxels);

        protected readonly NetDataWriter m_RejectionWriter = new NetDataWriter();
        protected AuthTicket m_SteamAuthenticationTicket;

        protected bool TryGetSteamAuthTicket()
        {
            if (m_SteamAuthenticationTicket != null) throw new Exception("Authentication ticket has already been obtained");
            if (SteamClient.IsValid && SteamClient.IsLoggedOn)
            {
                m_SteamAuthenticationTicket = SteamUser.GetAuthSessionTicket();
                return true;
            }
            return false;
        }

        protected override void OnRenderMode(Container session)
        {
            base.OnRenderMode(session);
            if (!IsLoading(session))
                foreach (ModelBehaviorBase modelBehavior in MapManager.Singleton.Models.Values)
                    modelBehavior.RenderContainer();
        }

        protected override void Dispose() => MapManager.Singleton.UnloadMap();

        public override void OnStop()
        {
            if (SteamClient.IsValid) m_SteamAuthenticationTicket?.Cancel();
        }

        protected override void OnSettingsTick(Container session)
        {
            MapManager.Singleton.SetMap(session.Require<VoxelMapNameProperty>());
            if (!IsLoading(session))
                foreach (ModelBehaviorBase modelBehavior in MapManager.Singleton.Models.Values)
                    modelBehavior.SetInMode(session);
        }

        public override bool IsLoading(Container session) => session.Require<VoxelMapNameProperty>() != MapManager.Singleton.Map.name
                                                          || ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;

        public override void OnThrowablePopped(ThrowableModifierBehavior throwableBehavior)
        {
            var center = (Position3Int) throwableBehavior.transform.position;
            SetVoxelRadius(center, throwableBehavior.Radius * 0.4f, true, true);
        }
    }
}