using Session;

namespace Compound.Session
{
    public class Client : ClientBase<SessionContainer, PlayerCommandsContainer>
    {
        public Client(IGameObjectLinker linker) : base(linker)
        {
        }
    }
}