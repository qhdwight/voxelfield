using System.Linq;
using UnityEngine;

namespace Swihoni.Sessions
{
    [CreateAssetMenu(fileName = "Session Linker", menuName = "Session/Linker", order = 0)]
    public class SessionGameObjectLinker : ScriptableObject
    {
        public static SessionGameObjectLinker Singleton { get; private set; }
        
        [RuntimeInitializeOnLoadMethod]
        public static void OnLoad() { Singleton = Resources.FindObjectsOfTypeAll<SessionGameObjectLinker>().FirstOrDefault(); }

        public GameObject PlayerModifierPrefab = default, PlayerVisualsPrefab = default;
    }
}