using Session;
using Session.Components;

namespace Compound.Session
{
    public class Server : ServerBase
    {
        public Server(IGameObjectLinker linker)
            : base(linker, StandardComponents.StandardSessionElements, StandardComponents.StandardPlayerElements, StandardComponents.StandardPlayerCommandsElements)
        {
        }
    }
}