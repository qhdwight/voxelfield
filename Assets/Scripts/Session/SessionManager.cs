using Compound;
using UnityEngine;

namespace Session
{
    public class SessionManager : SingletonBehavior<SessionManager>
    {
        private SessionBase m_Session;

        private void StartSession()
        {
            m_Session = new ClientSession();
            Time.fixedDeltaTime = 1.0f / 60.0f;
        }

        protected override void Awake()
        {
            base.Awake();
            StartSession();
        }

        private void Update()
        {
            m_Session?.HandleInput();
            m_Session?.Render();
        }

        private void FixedUpdate()
        {
            m_Session?.FixedUpdate();
        }
    }
}