using Session;
using Session.Components;

namespace Compound.Session
{
    public class Client : ClientBase
    {
        public Client(IGameObjectLinker linker)
            : base(linker, StandardComponents.StandardSessionElements, StandardComponents.StandardPlayerElements, StandardComponents.StandardPlayerCommandsElements)
        {
        }
    }
}