using System;
using UnityEngine;

namespace Swihoni.Util
{
    public static class TimeConversions
    {
        public const float MicrosecondToSecond = 1e-6f, SecondToMicrosecond = 1e6f;

        public static uint GetUsFromSecond(float seconds)
        {
            try
            {
                return checked((uint) Mathf.Round(SecondToMicrosecond * seconds));
            }
            catch (OverflowException)
            {
                Debug.LogWarning($"{seconds} caused overflow exception converting to Us");
                return 0u;
            }
        }
    }
}