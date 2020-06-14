using System.Diagnostics;
using UnityEngine;

namespace Swihoni.Util
{
    public static class EditorGraph
    {
        public const int Size = 240;
        public static readonly Keyframe[] Keys = new Keyframe[Size];
        public static float minValue, maxValue;

        [Conditional("UNITY_EDITOR")]
        public static void Next(float nextValue)
        {
            for (var i = 0; i < Size - 1; i++)
                Keys[i].value = Keys[i + 1].value;
            Keys[Size - 1].value = nextValue;

            minValue = float.MaxValue;
            maxValue = float.MinValue;
            for (var i = 0; i < Size; i++)
            {
                float value = Keys[i].value;
                if (value > maxValue)
                    maxValue = value;
                if (value < minValue)
                    minValue = value;
            }
        }
    }
}