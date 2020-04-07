using Session;

namespace Compound.Session
{
    public class Client : ClientBase<SessionComponent>
    {
        public Client(IGameObjectLinker linker) : base(linker)
        {
        }
    }
}