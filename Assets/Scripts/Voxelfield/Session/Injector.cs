#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_SERVER
#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Discord;
using LiteNetLib.Utils;
using Steamworks;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voxelfield.Integration;
using Voxels;
using Voxels.Map;
using Object = UnityEngine.Object;

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
        private const int DiscordUpdateRate = 10;

        protected readonly RequestConnectionComponent m_RequestConnection = new RequestConnectionComponent();
        protected readonly NetDataWriter m_RejectionWriter = new NetDataWriter();
        protected AuthTicket m_SteamAuthenticationTicket;
        
        protected MapManager m_MapManager;
        private long m_UnixStart;
        private bool m_IsLoading = true;

        public MapManager MapManager => m_MapManager;

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

            foreach (ModelBehaviorBase modelBehavior in m_MapManager.Models.Values)
                modelBehavior.RenderContainer();
        }

        protected override void OnDispose() => m_MapManager.SetMapToUnloaded();
        
        public override void OnStart()
        {
            var mapManagerPrefab = Resources.Load<GameObject>("Map Manager");
            GameObject mapManager = Object.Instantiate(mapManagerPrefab);
            SceneManager.MoveGameObjectToScene(mapManager, Session.Scene);
            mapManager.name = mapManagerPrefab.name;
            m_MapManager = mapManager.GetComponent<MapManager>();
            m_UnixStart = SessionExtensions.UnixNow;
        }

        public override void OnStop()
        {
            if (SteamClient.IsValid) m_SteamAuthenticationTicket?.Cancel();
        }

        protected virtual void OnMapChange() { }

        protected override void OnPreTick(Container session)
        {
            var mapName = session.Require<VoxelMapNameProperty>();
            if (m_MapManager.SetNamedMap(mapName)) OnMapChange();

            if (session.Require<ServerStampComponent>().tick % (session.Require<TickRateProperty>() * DiscordUpdateRate) == 0) OnModePeriodic(session);

            MapLoadingStage stage = m_MapManager.ChunkManager.ProgressInfo.stage;
            if (stage == MapLoadingStage.Failed) throw new Exception("Map failed to load");

            m_IsLoading = mapName != m_MapManager.Map.name || stage != MapLoadingStage.Completed;
            if (m_IsLoading) return;

            foreach (ModelBehaviorBase modelBehavior in m_MapManager.Models.Values)
                modelBehavior.SetInMode(session);
        }

        private void OnModePeriodic(Container session)
        {
            var state = new StringBuilder();
            state.AppendPropertyValue(session.Require<VoxelMapNameProperty>())
                 .Append(" - ")
                 .Append(ModeIdProperty.DisplayNames.GetForward(session.Require<ModeIdProperty>()));
            if (session.With(out DualScoresArray scores))
                state.Append(" - ").Append(scores[0].TryWithValue(out byte s1) && scores[1].TryWithValue(out byte s2) ? $"{s1} to {s2}" : "In Warmup");
            DiscordManager.SetActivity((ref Activity activity) =>
            {
                activity.State = state.ToString();
                activity.Timestamps = new ActivityTimestamps {Start = m_UnixStart};
            });
        }

        public override bool IsLoading(in SessionContext context) => m_IsLoading;

        protected void HandleMapReload(Container session)
        {
            var reload = session.Require<ReloadMapProperty>();
            if (reload.WithValueEqualTo(true))
            {
                m_MapManager.ReloadMap();
                reload.Clear();
            }
        }
    }
}