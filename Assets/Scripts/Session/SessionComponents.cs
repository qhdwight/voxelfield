using System;
using Collections;

namespace Session
{
    public class SessionComponents<TSessionComponent> : CyclicArray<TSessionComponent> where TSessionComponent : SessionComponentBase
    {
        public SessionComponents() : base(250, Activator.CreateInstance<TSessionComponent>)
        {
        }
    }
}