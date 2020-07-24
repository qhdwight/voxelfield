using UnityEngine;

namespace Swihoni.Util.Math
{
    public static class ExtraMath
    {
        public static float LateralMagnitude(this in Vector3 v) => Mathf.Sqrt(v.x * v.x + v.z * v.z);

        public static float Remap(this float value, float from1, float to1, float from2, float to2) => (value - from1) / (to1 - from1) * (to2 - from2) + from2;

        public static float SquareDistance(in Vector3 v1, in Vector3 v2) => (v1.x - v2.x) * (v1.x - v2.x) + (v1.y - v2.y) * (v1.y - v2.y) + (v1.z - v2.z) * (v1.z - v2.z);
    }
}