using Swihoni.Sessions;
using Swihoni.Sessions.Components;

namespace Compound.Session
{
    public class Host : HostBase
    {
        public Host()
            : base(StandardComponents.StandardSessionElements, StandardComponents.StandardPlayerElements, StandardComponents.StandardPlayerCommandsElements)
        {
        }
    }
}