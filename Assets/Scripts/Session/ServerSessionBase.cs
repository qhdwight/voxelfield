using System;
using UnityEngine;

namespace Session
{
    public class ServerSessionBase<TSessionState> : SessionBase<TSessionState> where TSessionState : SessionStateBase
    {
        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);

            float currentTime = Time.realtimeSinceStartup;
            var state = Activator.CreateInstance<TSessionState>();
            state.localPlayerId = 0;
            state.time = currentTime;
            state.duration = currentTime - m_States.Peek().time;
            state.tick = m_States.Peek().tick + 1;

            m_States.Add(state);
        }
    }
}