using Swihoni.Components;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class ThrowableVisualBehavior : EntityVisualBehavior
    {
        [SerializeField] private AudioSource m_PopAudioSource = default, m_ContinuousPopAudioSource = default, m_ContactAudioSource = default;
        protected ThrowableModifierBehavior m_Modifier;
        private ParticleSystem[] m_Particles;
        private uint m_LastThrownElapsedUs, m_LastContactElapsedUs;

        internal override void Setup(IBehaviorManager manager)
        {
            base.Setup(manager);
            m_Particles = GetComponentsInChildren<ParticleSystem>();
            m_Modifier = (ThrowableModifierBehavior) m_Manager.GetModifierPrefab(id);
            foreach (ParticleSystem particle in m_Particles) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public override void SetActive(bool isActive)
        {
            if (isActive)
            {
                m_LastContactElapsedUs = 0u;
            }
            if (m_ContinuousPopAudioSource) m_ContinuousPopAudioSource.Stop();
        }

        public override void Render(Container entity)
        {
            base.Render(entity);

            var throwable = entity.Require<ThrowableComponent>();

            bool hasPopped = throwable.thrownElapsedUs >= throwable.popTimeUs;

            if (hasPopped)
            {
                bool hasJustPopped = m_LastThrownElapsedUs < throwable.popTimeUs;
                if (hasJustPopped) m_PopAudioSource.PlayOneShot(m_PopAudioSource.clip, 1.0f);
                uint particleElapsedUs = throwable.thrownElapsedUs - throwable.popTimeUs;
                foreach (ParticleSystem particle in m_Particles)
                {
                    particle.time = particleElapsedUs * TimeConversions.MicrosecondToSecond;
                    if (particle.isStopped && hasJustPopped) particle.Play(false);
                }
                if (m_ContinuousPopAudioSource && !m_ContinuousPopAudioSource.isPlaying) m_ContinuousPopAudioSource.Play();
            }
            else
            {
                if (throwable.contactElapsedUs < m_LastContactElapsedUs)
                    m_ContactAudioSource.PlayOneShot(m_ContactAudioSource.clip, 1.0f);
                if (m_ContinuousPopAudioSource) m_ContinuousPopAudioSource.Stop();
            }
            m_LastThrownElapsedUs = throwable.thrownElapsedUs;
            m_LastContactElapsedUs = throwable.contactElapsedUs;
        }

        public override bool IsVisible(Container entity)
        {
            var throwable = entity.Require<ThrowableComponent>();
            return throwable.thrownElapsedUs < throwable.popTimeUs;
        }
    }
}