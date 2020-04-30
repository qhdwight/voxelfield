using Swihoni.Collections;
using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Player
{
    public class PlayerVisualizerBehavior : PlayerVisualizerBase
    {
        private static StrictPool<PlayerVisualizerBehavior> _pool;

        [RuntimeInitializeOnLoadMethod]
        private static void Load()
        {
            _pool = new StrictPool<PlayerVisualizerBehavior>(8, () =>
            {
                GameObject instance = Instantiate(SessionGameObjectLinker.Singleton.GetPlayerVisualsPrefab());
                var visualizer = instance.GetComponent<PlayerVisualizerBehavior>();
                instance.SetActive(false);
                return visualizer;
            });
        }

        public static void Render(SessionBase session, int playerId, Container player)
        {
            PlayerVisualizerBehavior visualizer = _pool.Obtain();
            visualizer.gameObject.SetActive(true);
            visualizer.Setup(session);
            visualizer.Evaluate(playerId, player);
        }
    }
}