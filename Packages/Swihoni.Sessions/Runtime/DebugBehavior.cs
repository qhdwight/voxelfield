using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Util;

namespace Swihoni.Sessions
{
    public class DebugBehavior : SingletonBehavior<DebugBehavior>
    {
        public FloatProperty RollbackOverride;

        public Container Predicted;

        public SessionSettingsComponent Settings;

        public Container RenderOverride;
    }
}