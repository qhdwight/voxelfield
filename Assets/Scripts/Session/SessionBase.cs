using System;
using Collections;
using Components;
using Session.Player.Components;
using UnityEngine;

namespace Session
{
    public abstract class SessionBase<TSessionComponent> where TSessionComponent : SessionComponentBase
    {
        protected readonly SessionComponents<TSessionComponent> m_SessionComponents;
        private float m_FixedUpdateTime;
        protected SessionSettingsComponent m_Settings = DebugBehavior.Singleton.Settings;
        protected uint m_Tick;

        protected SessionBase()
        {
            m_SessionComponents = new SessionComponents<TSessionComponent>();
        }

        public void Render()
        {
            Render(Time.realtimeSinceStartup - m_FixedUpdateTime);
        }

        protected virtual void Render(float timeSinceTick)
        {
        }

        protected virtual void Tick(uint tick, float time)
        {
            Time.fixedDeltaTime = 1.0f / m_Settings.tickRate;
        }

        public virtual void HandleInput()
        {
        }

        public void FixedUpdate()
        {
            Tick(m_Tick++, m_FixedUpdateTime = Time.realtimeSinceStartup);
        }

        protected bool InterpolateHistoryInto(PlayerComponent componentToInterpolate, CyclicArray<StampedPlayerComponent> componentHistory,
                                              float rollback, float timeSinceLastUpdate)
        {
            StampedPlayerComponent fromComponent = null, toComponent = null;
            var durationCount = 0.0f;
            for (var componentHistoryIndex = 0; componentHistoryIndex < componentHistory.Size; componentHistoryIndex++)
            {
                fromComponent = componentHistory.Get(-componentHistoryIndex - 1);
                toComponent = componentHistory.Get(-componentHistoryIndex);
                durationCount += toComponent.duration;
                if (durationCount >= rollback - timeSinceLastUpdate) break;
                if (componentHistoryIndex != componentHistory.Size - 1) continue;
                // We do not have enough history. Copy the most recent instead
                Copier.CopyTo(componentHistory.Peek().component, componentToInterpolate);
                return false;
            }
            if (toComponent == null)
                throw new ArgumentException("Cyclic array is not big enough");
            float interpolation;
            if (toComponent.duration > 0.0f)
            {
                float elapsed = durationCount - rollback + timeSinceLastUpdate;
                interpolation = elapsed / toComponent.duration;
            }
            else
                interpolation = 0.0f;
            Interpolator.InterpolateInto(fromComponent.component, toComponent.component, componentToInterpolate, interpolation);
            return true;
        }

        protected class StampedPlayerComponent : StampComponent
        {
            public PlayerComponent component;
        }
    }
}