using Session;
using Session.Components;

namespace Compound.Session
{
    public class Host : HostBase
    {
        public Host(IGameObjectLinker linker)
            : base(linker, StandardComponents.StandardSessionComponents, StandardComponents.StandardPlayerComponents, StandardComponents.StandardPlayerCommandsComponents)
        {
        }
    }
}