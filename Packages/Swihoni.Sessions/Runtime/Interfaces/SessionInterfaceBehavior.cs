using Swihoni.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public abstract class SessionInterfaceBehavior : InterfaceBehaviorBase
    {
        [SerializeField] private bool m_IsDuringGame = true;

        public bool IsDuringGame => m_IsDuringGame;

        public abstract void Render(SessionBase session, Container sessionContainer);

        public virtual void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands) { }

        public virtual void SessionStateChange(bool isActive)
        {
            if (m_IsDuringGame && isActive) SetInterfaceActive(false);
        }

        protected bool NoInterrupting => !SessionBase.InterruptingInterface || SessionBase.InterruptingInterface == this;
    }
}