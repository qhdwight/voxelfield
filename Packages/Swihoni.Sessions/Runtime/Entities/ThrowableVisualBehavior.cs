using Swihoni.Components;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class ThrowableVisualBehavior : EntityVisualBehavior
    {
        [SerializeField] private AudioClip m_PopAudioClip = default, m_ContactAudioClip = default;
        protected ThrowableModifierBehavior m_Modifier;
        private AudioSource m_AudioSource;
        private ParticleSystem[] m_Particles;
        private uint m_LastThrownElapsedUs, m_LastContactElapsedUs;

        internal override void Setup(IBehaviorManager manager)
        {
            base.Setup(manager);
            m_Particles = GetComponentsInChildren<ParticleSystem>();
            m_AudioSource = GetComponent<AudioSource>();
            m_Modifier = (ThrowableModifierBehavior) m_Manager.GetModifierPrefab(id);
            foreach (ParticleSystem particle in m_Particles) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_LastContactElapsedUs = 0u;
        }

        public override void Render(Container entity)
        {
            base.Render(entity);

            var throwable = entity.Require<ThrowableComponent>();

            bool hasPopped = throwable.thrownElapsedUs > throwable.popTimeUs;

            if (hasPopped)
            {
                bool hasJustPopped = m_LastThrownElapsedUs < throwable.popTimeUs;
                if (hasJustPopped) m_AudioSource.PlayOneShot(m_PopAudioClip, 1.0f);
                uint particleElapsedUs = throwable.thrownElapsedUs - throwable.popTimeUs;
                foreach (ParticleSystem particle in m_Particles)
                {
                    particle.time = particleElapsedUs * TimeConversions.MicrosecondToSecond;
                    if (particle.isStopped) particle.Play(false);
                }
            }
            else
            {
                if (throwable.contactElapsedUs < m_LastContactElapsedUs && m_LastContactElapsedUs > 0.1f)
                    m_AudioSource.PlayOneShot(m_ContactAudioClip, 1.0f);
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