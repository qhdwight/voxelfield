using UnityEngine;

namespace Session
{
    public abstract class SessionBase
    {
        protected readonly SessionStates m_States = new SessionStates(250);
        protected uint m_Tick;

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