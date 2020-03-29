using System;
using Collections;

namespace Session
{
    public class SessionStates<TSessionState> : CyclicArray<TSessionState> where TSessionState : SessionStateBase
    {
        public SessionStates() : base(250, Activator.CreateInstance<TSessionState>)
        {
        }
    }
}