using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions
{
    public class DebugBehavior : SingletonBehavior<DebugBehavior>
    {
        [Range(0.0f, 1.0f)] public float Rollback;

        public Container Predicted;

        public SessionSettingsComponent Settings;

        public Container RenderOverride;
    }
}