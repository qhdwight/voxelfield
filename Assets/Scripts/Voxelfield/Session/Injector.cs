#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_SERVER
#endif

using System;
using LiteNetLib.Utils;
using Steamworks;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using Voxelfield.Integration;
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

        private bool m_IsLoading = true;
        private readonly ModeIdProperty m_PreviousMode = new ModeIdProperty();

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize() => SessionBase.RegisterSessionCommand("reload_map");

        protected bool TryGetSteamAuthTicket()
        {
            if (m_SteamAuthenticationTicket != null) throw new Exception("Authentication ticket has already been obtained");
            if (!SteamClient.IsValid || !SteamClient.IsLoggedOn) return false;

            m_SteamAuthenticationTicket = SteamUser.GetAuthSessionTicket();
            return true;
        }

        protected override void OnRenderMode(in SessionContext context)
        {
            base.OnRenderMode(in context);
            if (IsLoading(context)) return;

            foreach (ModelBehaviorBase modelBehavior in MapManager.Singleton.Models.Values)
                modelBehavior.RenderContainer();
        }

        protected override void OnDispose() => MapManager.Singleton.SetMapToUnloaded();

        public override void OnStop()
        {
            if (SteamClient.IsValid) m_SteamAuthenticationTicket?.Cancel();
        }

        protected virtual void OnMapChange() { }

        protected override void OnPreTick(Container session)
        {
            var mapName = session.Require<VoxelMapNameProperty>();
            if (MapManager.Singleton.SetNamedMap(mapName)) OnMapChange();

            if (session.Require<ModeIdProperty>().CompareUpdate(m_PreviousMode)) OnModeChange(session);

            MapLoadingStage stage = ChunkManager.Singleton.ProgressInfo.stage;
            if (stage == MapLoadingStage.Failed) throw new Exception("Map failed to load");

            m_IsLoading = mapName != MapManager.Singleton.Map.name || stage != MapLoadingStage.Completed;
            if (m_IsLoading) return;

            foreach (ModelBehaviorBase modelBehavior in MapManager.Singleton.Models.Values)
                modelBehavior.SetInMode(session);
        }

        public override void OnPostTick(Container session)
        {
            var reload = session.Require<ReloadMapProperty>();
            if (reload.WithValueEqualTo(true))
            {
                MapManager.ReloadMap();
                reload.Clear();
            }
        }

        private static void OnModeChange(Container session)
            => DiscordManager.SetActivity($"In Match - {ModeIdProperty.DisplayNames.GetForward(session.Require<ModeIdProperty>())}");

        public override bool IsLoading(in SessionContext context) => m_IsLoading;
    }
}