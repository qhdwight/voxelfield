using System;
using UnityEngine;

namespace Swihoni.Util
{
    public static class ExceptionLogger
    {
        public static void Log(Exception exception, string prefix)
        {
#if VOXELFIELD_RELEASE
            Debug.LogError($"{prefix}: {exception.Message}");
#else
            Debug.LogError($"{prefix}: {exception}");
#endif
        }
    }
}