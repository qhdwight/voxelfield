using Swihoni.Sessions;
using Swihoni.Sessions.Components;

namespace Compound.Session
{
    public class Host : HostBase
    {
        public Host(ISessionGameObjectLinker linker)
            : base(linker, StandardComponents.StandardSessionElements, StandardComponents.StandardPlayerElements, StandardComponents.StandardPlayerCommandsElements)
        {
        }
    }
}