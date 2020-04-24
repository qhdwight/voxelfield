using System.Net;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;

namespace Compound.Session
{
    public class Client : ClientBase
    {
        public Client(IGameObjectLinker linker, IPEndPoint ipEndPoint)
            : base(linker, ipEndPoint, StandardComponents.StandardSessionElements, StandardComponents.StandardPlayerElements, StandardComponents.StandardPlayerCommandsElements)
        {
        }
    }
}