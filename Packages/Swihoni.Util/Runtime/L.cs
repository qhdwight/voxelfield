using System;
using UnityEngine;

namespace Swihoni.Util
{
    public static class L
    {
        public static void Exception(Exception exception, string prefix)
        {
#if VOXELFIELD_RELEASE
            Debug.LogError($"{prefix}: {exception.Message}");
#else
            Debug.LogError($"{prefix}: {exception}");
#endif
        }

        public static void Warning(string message)
        {
#if !VOXELFIELD_RELEASE_CLIENT || UNITY_EDITOR
            Debug.LogWarning(message);
#endif
        }
    }
}