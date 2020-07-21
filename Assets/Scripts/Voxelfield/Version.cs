using System.Linq;
using UnityEngine;

namespace Voxelfield
{
    public static class Version
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            if (!Application.isBatchMode) return;
            
            string message = $"Starting voxelfield version v{Application.version}",
                   separator = string.Concat(Enumerable.Repeat("=", message.Length));
            Debug.Log(separator);
            Debug.Log(message);
            Debug.Log(separator);
        }
    }
}