using System.Net;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;

namespace Compound.Session
{
    public class Client : ClientBase
    {
        public Client(IPEndPoint ipEndPoint)
            : base(ipEndPoint, StandardComponents.StandardSessionElements, StandardComponents.StandardPlayerElements, StandardComponents.StandardPlayerCommandsElements)
        {
        }
    }
}