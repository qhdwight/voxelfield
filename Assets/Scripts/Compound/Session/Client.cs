using Session;

namespace Compound.Session
{
    public class Client : ClientBase
    {
        public Client(IGameObjectLinker linker) : base(linker, typeof(SessionContainer), typeof(PlayerContainer), typeof(PlayerCommandsContainer))
        {
        }
    }
}