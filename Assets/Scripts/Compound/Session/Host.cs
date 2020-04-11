using Session;

namespace Compound.Session
{
    public class Host : HostBase
    {
        public Host(IGameObjectLinker linker) : base(linker, typeof(SessionContainer), typeof(PlayerContainer), typeof(PlayerCommandsContainer))
        {
        }
    }
}