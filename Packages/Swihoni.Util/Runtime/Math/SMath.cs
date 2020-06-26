using UnityEngine;

namespace Swihoni.Util.Math
{
    public static class SMath
    {
        public static float LateralMagnitude(this Vector3 v) => Mathf.Sqrt(v.x * v.x + v.z * v.z);

        public static float Remap(this float value, float from1, float to1, float from2, float to2) => (value - from1) / (to1 - from1) * (to2 - from2) + from2;
    }
}