#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_SERVER
#endif

using System;
using LiteNetLib.Utils;
using Steamworks;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Entities;
using Swihoni.Util.Math;
using Voxels;
using Voxels.Map;

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

        protected override void OnDispose() => MapManager.Singleton.UnloadMap();

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
    }
}