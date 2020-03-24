using UnityEngine;

namespace Session
{
    public class ServerSession : SessionBase
    {
        public override void Tick()
        {
            base.Tick();

            float currentTime = Time.realtimeSinceStartup;
            var state = new SessionState {localPlayerId = 0, time = currentTime, duration = currentTime - m_States.Peek()?.time ?? 0.0f, tick = m_States.Peek()?.tick ?? 0 + 1};

            m_States.Add(state);
        }
    }
}