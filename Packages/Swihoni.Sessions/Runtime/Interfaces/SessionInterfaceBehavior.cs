using Swihoni.Components;
using Swihoni.Util.Interface;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class SessionInterfaceBehavior : InterfaceBehaviorBase
    {
        public abstract void Render(Container session);
    }
}