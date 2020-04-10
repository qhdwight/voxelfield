using Session;

namespace Compound.Session
{
    public class Host : HostBase<SessionContainer>
    {
        public Host(IGameObjectLinker linker) : base(linker)
        {
        }
    }
}