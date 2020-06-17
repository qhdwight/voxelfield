using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions
{
    public class DebugBehavior : SingletonBehavior<DebugBehavior>
    {
        public bool isDebugMode;

        private StrictPool<PlayerVisualizerBehavior> m_Pool;

        public UIntProperty RollbackOverrideUs;

        public TickRateProperty TickRate;

        public ModeIdProperty ModeId;

        public string mapName;

        private static StrictPool<PlayerVisualizerBehavior> CreatePool() =>
            new StrictPool<PlayerVisualizerBehavior>(16, () =>
            {
                GameObject instance = Instantiate(SessionGameObjectLinker.Singleton.GetPlayerVisualizerPrefab());
                var visualizer = instance.GetComponent<PlayerVisualizerBehavior>();
                visualizer.Setup();
                instance.SetActive(false);
                return visualizer;
            });

        private void OnApplicationQuit()
        {
            if (m_Pool == null) return;
            m_Pool.ReturnAll();
            foreach (PlayerVisualizerBehavior visual in m_Pool)
                visual.Dispose();
        }

        public void Render(SessionBase session, int playerId, Container player, Color color)
        {
            if (!isDebugMode) return;
            if (m_Pool == null) m_Pool = CreatePool();
            PlayerVisualizerBehavior visualizer = m_Pool.Obtain();
            visualizer.gameObject.SetActive(true);
            visualizer.Setup(session);
            visualizer.Evaluate(playerId, player);
            visualizer.Renderer.material.color = color;
        }
    }
}