using Swihoni.Components;
using Swihoni.Util.Interface;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class SessionInterfaceBehavior : InterfaceBehaviorBase
    {
        public abstract void Render(SessionBase session, Container sessionContainer);

        public virtual void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands) { }

        public virtual void SessionStateChange(bool isActive)
        {
            if (!isActive) SetInterfaceActive(false);
        }

        protected bool NoInterrupting(SessionBase session) => !session.InterruptingInterface || session.InterruptingInterface == this;
    }
}