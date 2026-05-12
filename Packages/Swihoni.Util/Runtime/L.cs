using System;
using UnityEngine;

namespace Swihoni.Util
{
    public static class L
    {
        public static void Exception(Exception exception, string prefix)
        {
            Debug.LogError($"{prefix}: {exception}");
        }

        public static void Warning(string message)
        {
#if UNITY_EDITOR
            Debug.LogWarning(message);
#endif
        }
    }
}