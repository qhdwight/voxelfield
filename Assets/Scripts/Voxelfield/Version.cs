using System.Linq;
using UnityEngine;

namespace Voxelfield
{
    public static class Version
    {
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            string message = $"Starting voxelfield version v{String}",
                   separator = string.Concat(Enumerable.Repeat("=", message.Length));
            Debug.Log(separator);
            Debug.Log(message);
            Debug.Log(separator);
        }
        
        public const string String = "0.0.7";
    }
}