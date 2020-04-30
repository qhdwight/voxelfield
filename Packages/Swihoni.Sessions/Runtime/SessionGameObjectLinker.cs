using UnityEngine;

namespace Swihoni.Sessions
{
    public interface ISessionGameObjectLinker
    {
        GameObject GetPlayerModifierPrefab();

        GameObject GetPlayerVisualsPrefab();

        GameObject GetPlayerVisualizerPrefab();
    }

    [CreateAssetMenu(fileName = "Session Linker", menuName = "Session/Linker")]
    public class SessionGameObjectLinker : ScriptableObject, ISessionGameObjectLinker
    {
        public static SessionGameObjectLinker Singleton { get; private set; }

        [SerializeField] private GameObject m_PlayerModifierPrefab = default,
                                            m_PlayerVisualsPrefab = default,
                                            m_PlayerVisualizerPrefab = default;

        private void OnEnable() => Singleton = this;

        public GameObject GetPlayerModifierPrefab() => m_PlayerModifierPrefab;

        public GameObject GetPlayerVisualsPrefab() => m_PlayerVisualsPrefab;

        public GameObject GetPlayerVisualizerPrefab() => m_PlayerVisualizerPrefab;
    }
}