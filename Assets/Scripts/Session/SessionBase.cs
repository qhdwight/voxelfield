using Session.Player;

namespace Session
{
    public abstract class SessionBase
    {
        protected readonly SessionStates m_States = new SessionStates(250);

        public virtual void Render()
        {
            PlayerManager.Singleton.Visualize(m_States.Peek());
        }

        public virtual void Tick()
        {
        }

        public virtual void HandleInput()
        {
        }
    }
}