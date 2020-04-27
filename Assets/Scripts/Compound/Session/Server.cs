using Swihoni.Sessions;
using Swihoni.Sessions.Components;

namespace Compound.Session
{
    public class Server : ServerBase
    {
        public Server()
            : base(StandardComponents.StandardSessionElements, StandardComponents.StandardPlayerElements, StandardComponents.StandardPlayerCommandsElements)
        {
        }
    }
}