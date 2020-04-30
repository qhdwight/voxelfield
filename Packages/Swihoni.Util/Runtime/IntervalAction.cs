using System;

namespace Swihoni.Util
{
    public class IntervalAction
    {
        private float m_TimeSinceLastInterval;
        private readonly float m_Interval;
        private readonly Action m_Action;

        public IntervalAction(float interval, Action action)
        {
            m_Interval = interval;
            m_Action = action;
        }

        public void Update(float deltaTime)
        {
            if (m_Interval <= 0.0f) return;
            m_TimeSinceLastInterval += deltaTime;
            while (m_TimeSinceLastInterval > m_Interval)
            {
                m_Action();
                m_TimeSinceLastInterval -= m_Interval;
            }
        }
    }
}