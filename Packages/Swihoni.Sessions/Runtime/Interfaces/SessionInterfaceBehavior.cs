using Swihoni.Components;
using Swihoni.Util.Interface;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class SessionInterfaceBehavior : InterfaceBehaviorBase
    {
        public abstract void Render(SessionBase session, Container sessionContainer);

        public virtual void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands) { }

        public virtual void SessionStateChange(bool isActive) => SetInterfaceActive(isActive);
    }
}