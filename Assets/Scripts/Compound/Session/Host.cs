using Session;

namespace Compound.Session
{
    public class Host : HostBase<SessionComponent>
    {
        public Host(IGameObjectLinker linker) : base(linker)
        {
        }
    }
}