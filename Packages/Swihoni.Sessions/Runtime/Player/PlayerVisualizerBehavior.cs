using Swihoni.Collections;
using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public class PlayerVisualizerBehavior : PlayerVisualizerBase
    {
        private static StrictPool<PlayerVisualizerBehavior> _pool;
        private SkinnedMeshRenderer m_Renderer;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize() { _pool = null; }

        public static void Render(SessionBase session, int playerId, Container player, Color color)
        {
            if (_pool == null) _pool = CreatePool();
            PlayerVisualizerBehavior visualizer = _pool.Obtain();
            visualizer.gameObject.SetActive(true);
            visualizer.Setup(session);
            visualizer.Evaluate(playerId, player);
            visualizer.m_Renderer.material.color = color;
        }

        private static StrictPool<PlayerVisualizerBehavior> CreatePool()
        {
            return new StrictPool<PlayerVisualizerBehavior>(16, () =>
            {
                GameObject instance = Instantiate(SessionGameObjectLinker.Singleton.GetPlayerVisualizerPrefab());
                var visualizer = instance.GetComponent<PlayerVisualizerBehavior>();
                visualizer.m_Renderer = instance.GetComponentInChildren<SkinnedMeshRenderer>();
                instance.SetActive(false);
                return visualizer;
            });
        }
    }
}