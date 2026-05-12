using System;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Sessions;
using UnityEngine;
using UnityEngine.SceneManagement;
using Voxels;
using Voxels.Map;
using Object = UnityEngine.Object;

namespace Voxelfield.Session
{
    [Serializable]
    public class RequestConnectionComponent : ComponentBase
    {
        public StringProperty version;
    }

    public class Injector : SessionInjectorBase
    {
        protected readonly RequestConnectionComponent m_RequestConnection = new();
        protected readonly NetDataWriter m_RejectionWriter = new();

        protected MapManager m_MapManager;
        private bool m_IsLoading = true;

        public MapManager MapManager => m_MapManager;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize() => SessionBase.RegisterSessionCommand("reload_map");

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
        }
        
        protected virtual void OnMapChange() { }

        protected override void OnPreTick(Container session)
        {
            session.Require<MapGenerationProperty>().SetValueIfWithout();

            var mapName = session.Require<VoxelMapNameProperty>();
            if (m_MapManager.SetNamedMap(mapName)) OnMapChange();

            MapLoadingStage stage = m_MapManager.ChunkManager.ProgressInfo.stage;
            if (stage == MapLoadingStage.Failed) throw new Exception("Map failed to load");

            m_IsLoading = mapName != m_MapManager.Map.name || stage != MapLoadingStage.Completed;
            if (m_IsLoading) return;

            foreach (ModelBehaviorBase modelBehavior in m_MapManager.Models.Values)
                modelBehavior.SetInMode(session);
        }

        public override bool IsLoading(in SessionContext context) => m_IsLoading;

        protected void HandleMapReload(Container session) =>
            m_MapManager.SetGeneration(session.Require<MapGenerationProperty>());
    }
}