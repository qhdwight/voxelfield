using System;
using UnityEngine;

namespace Session
{
    public class ServerSessionBase<TSessionState> : SessionBase<TSessionState> where TSessionState : SessionStateComponentBase
    {
        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            float currentTime = Time.realtimeSinceStartup;
            var state = Activator.CreateInstance<TSessionState>();
            state.localPlayerId.Value = 0;
            state.time.Value = currentTime;
            state.duration.Value = currentTime - m_States.Peek().time;
            state.tick.Value = m_States.Peek().tick + 1;

            m_States.Add(state);
        }
    }
}