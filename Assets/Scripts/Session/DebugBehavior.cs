using Session.Player.Components;
using UnityEngine;
using Util;

namespace Session
{
    public class DebugBehavior : SingletonBehavior<DebugBehavior>
    {
        [Range(0.0f, 1.0f)] public float Rollback;

        public PlayerComponent Predicted;

        public SessionComponentBase Server;

        public SessionSettingsComponent Settings;

        public PlayerComponent RenderOverride;
    }
}