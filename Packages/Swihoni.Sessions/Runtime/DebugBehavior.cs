using System.Linq;
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
        private static GameObject _visualizerPrefab;

        private StrictPool<PlayerVisualizerBehavior> m_Pool;
        
        public bool IsDebugMode;
        public UIntProperty RollbackOverrideUs;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => _visualizerPrefab = Resources.LoadAll<GameObject>("Players")
                                                                         .First(gameObject => gameObject.GetComponent<PlayerVisualizerBehavior>() != null);

        private static StrictPool<PlayerVisualizerBehavior> CreatePool() =>
            new StrictPool<PlayerVisualizerBehavior>(16, () =>
            {
                GameObject instance = Instantiate(_visualizerPrefab);
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
            if (!IsDebugMode) return;
            if (m_Pool == null) m_Pool = CreatePool();
            PlayerVisualizerBehavior visualizer = m_Pool.Obtain();
            visualizer.gameObject.SetActive(true);
            visualizer.Setup(session);
            visualizer.Evaluate(session, playerId, player);
            visualizer.Renderer.material.color = color;
        }
    }
}