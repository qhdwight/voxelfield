using System;
using Collections;

namespace Session
{
    public class SessionStates<TSessionState> : CyclicArray<TSessionState> where TSessionState : SessionStateComponentBase
    {
        public SessionStates() : base(250, Activator.CreateInstance<TSessionState>)
        {
        }
    }
}