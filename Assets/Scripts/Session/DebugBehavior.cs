using Components;
using Session.Components;
using UnityEngine;
using Util;

namespace Session
{
    public class DebugBehavior : SingletonBehavior<DebugBehavior>
    {
        [Range(0.0f, 1.0f)] public float Rollback;

        public Container Predicted;

        public SessionSettingsComponent Settings;

        public Container RenderOverride;
    }
}