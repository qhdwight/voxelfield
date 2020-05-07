using System.Net;
using Swihoni.Sessions;

namespace Compound.Session
{
    public class Client : ClientBase
    {
        public Client(IPEndPoint ipEndPoint)
            : base(CompoundComponents.SessionElements, ipEndPoint)
        {
        }
    }
}