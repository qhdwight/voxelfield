using UnityEngine;

namespace Swihoni.Util
{
    public static class VectorMath
    {
        public static float LateralMagnitude(Vector3 v) { return Mathf.Sqrt(v.x * v.x + v.z * v.z); }
    }
}