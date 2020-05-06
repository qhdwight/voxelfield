using System;

namespace Swihoni.Util
{
    public class ElapseWatcher
    {
        public float m_LastTime;

        public void Reset()
        {
            m_LastTime = 0.0f;
        }
        
        public void Update(float time, float target, Action action)
        {
            if (time > target && m_LastTime < target)
                action();
            m_LastTime = time;
        }
    }
}