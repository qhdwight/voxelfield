using System;
using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions
{
    [DefaultExecutionOrder(50)]
    public class DebugBehavior : SingletonBehavior<DebugBehavior>
    {
        private static GameObject _visualizerPrefab;

        [SerializeField] private string m_AutoCommand = string.Empty;

        private StrictPool<PlayerDebugVisualizerBehavior> m_Pool;

        public bool SendDebug;
        public UIntProperty RollbackOverrideUs;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize() => _visualizerPrefab = Resources.LoadAll<GameObject>("Players")
                                                                         .First(gameObject => gameObject.GetComponent<PlayerDebugVisualizerBehavior>() != null);

        private static StrictPool<PlayerDebugVisualizerBehavior> CreatePool() =>
            new(16, () =>
            {
                GameObject instance = Instantiate(_visualizerPrefab);
                var visualizer = instance.GetComponent<PlayerDebugVisualizerBehavior>();
                visualizer.Setup();
                instance.SetActive(false);
                return visualizer;
            });

        private void Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            for (var i = 0; i < arguments.Length; i++)
                if (arguments[i] == "-c")
                    m_AutoCommand = arguments[i + 1];

            if (string.IsNullOrEmpty(m_AutoCommand)) return;

            ConsoleCommandExecutor.ExecuteCommand(m_AutoCommand);
        }

        private void OnApplicationQuit()
        {
            if (m_Pool == null) return;
            m_Pool.ReturnAll();
            foreach (PlayerDebugVisualizerBehavior visual in m_Pool)
                visual.Dispose();
        }

        public void Render(in SessionContext context, in Color color)
        {
            if (!SendDebug) return;
            if (m_Pool == null) m_Pool = CreatePool();
            PlayerDebugVisualizerBehavior debugVisualizer = m_Pool.Obtain();
            debugVisualizer.gameObject.SetActive(true);
            debugVisualizer.Setup();
            debugVisualizer.Evaluate(context);
            debugVisualizer.BodyAnimator.SetColor(color);
        }
    }
}