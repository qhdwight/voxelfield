using UnityEngine;

namespace Session
{
    public abstract class SessionBase<TSessionState> where TSessionState : SessionStateBase
    {
        protected readonly SessionStates<TSessionState> m_States;
        protected uint m_Tick;

        protected SessionBase()
        {
            m_States = new SessionStates<TSessionState>();
        }

        public virtual void Render()
        {
        }

        protected virtual void Tick(uint tick, float time)
        {
        }

        public virtual void HandleInput()
        {
        }

        public void FixedUpdate()
        {
            Tick(m_Tick++, Time.realtimeSinceStartup);
        }
    }
}