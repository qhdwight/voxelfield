using Swihoni.Sessions;

namespace Compound.Session
{
    public class Server : ServerBase
    {
        public Server()
            : base(CompoundComponents.SessionElements)
        {
        }
    }
}