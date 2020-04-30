using UnityEngine;

namespace Swihoni.Sessions
{
    public interface ISessionGameObjectLinker
    {
        GameObject GetPlayerModifierPrefab();

        GameObject GetPlayerVisualsPrefab();
    }

    [CreateAssetMenu(fileName = "Session Linker", menuName = "Session/Linker")]
    public class SessionGameObjectLinker : ScriptableObject, ISessionGameObjectLinker
    {
        public static SessionGameObjectLinker Singleton { get; private set; }

        [SerializeField] private GameObject m_PlayerModifierPrefab = default, m_PlayerVisualsPrefab = default;

        private void OnEnable() { Singleton = this; }

        public GameObject GetPlayerModifierPrefab() { return m_PlayerModifierPrefab; }

        public GameObject GetPlayerVisualsPrefab() { return m_PlayerVisualsPrefab; }
    }
}