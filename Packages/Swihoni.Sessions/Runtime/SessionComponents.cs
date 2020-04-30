using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Sessions.Components;

namespace Swihoni.Sessions
{
    /* Server */

    [Serializable]
    public class ServerSessionContainer : Container
    {
        public ServerSessionContainer() { }
        public ServerSessionContainer(IEnumerable<Type> types) : base(types) { }
    }

    [Serializable]
    public class ServerStampComponent : StampComponent
    {
    }

    [Serializable]
    public class ServerTag : ComponentBase
    {
    }

    /* Client */

    [Serializable]
    public class ClientCommandsContainer : Container
    {
        public ClientCommandsContainer() { }
        public ClientCommandsContainer(IEnumerable<Type> types) : base(types) { }
    }

    [Serializable]
    public class ClientStampComponent : StampComponent
    {
    }

    /// <summary>
    /// Use to put server time of updates into client time.
    /// </summary>
    [Serializable]
    public class LocalizedClientStampComponent : StampComponent
    {
    }

    /* Shared */

    [Serializable]
    public class HitMarkerComponent : ComponentBase
    {
        public FloatProperty elapsed;
        public BoolProperty isKill;
    }
}