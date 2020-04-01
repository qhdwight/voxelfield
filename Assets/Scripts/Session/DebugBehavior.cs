using UnityEngine;
using Util;

namespace Session
{
    public class DebugBehavior : SingletonBehavior<DebugBehavior>
    {
        [Range(0.0f, 1.0f)] public float Rollback;
        [Range(1, 120)] public byte TickRate;
    }
}