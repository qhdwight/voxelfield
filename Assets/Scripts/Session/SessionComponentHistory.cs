using System;
using Collections;

namespace Session
{
    public class SessionComponentHistory<TSessionComponent> : CyclicArray<TSessionComponent> where TSessionComponent : SessionComponentBase
    {
        public SessionComponentHistory() : base(250, Activator.CreateInstance<TSessionComponent>)
        {
        }
    }
}