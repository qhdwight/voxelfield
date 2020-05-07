using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class ThrowableVisualBehavior : EntityVisualBehavior
    {
        [SerializeField] private AudioClip m_PopAudioClip = default, m_ContactAudioClip = default;
        protected ThrowableModifierBehavior m_Modifier;
        private AudioSource m_AudioSource;
        private ParticleSystem[] m_Particles;
        private float m_LastThrownElapsed, m_LastContactElapsed;

        internal override void Setup(EntityManager manager)
        {
            base.Setup(manager);
            m_Particles = GetComponentsInChildren<ParticleSystem>();
            m_AudioSource = GetComponent<AudioSource>();
            m_Modifier = (ThrowableModifierBehavior) m_Manager.GetModifierPrefab(id);
            foreach (ParticleSystem particle in m_Particles) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
            m_LastContactElapsed = float.NegativeInfinity;
        }

        public override void Render(EntityContainer entity)
        {
            base.Render(entity);

            var throwable = entity.Require<ThrowableComponent>();

            bool hasPopped = throwable.thrownElapsed > throwable.popTime;

            if (hasPopped)
            {
                bool hasJustPopped = m_LastThrownElapsed < throwable.popTime;
                if (hasJustPopped) m_AudioSource.PlayOneShot(m_PopAudioClip, 1.0f);
                float particleElapsed = throwable.thrownElapsed - throwable.popTime;
                foreach (ParticleSystem particle in m_Particles)
                {
                    particle.time = particleElapsed;
                    if (particle.isStopped) particle.Play(false);
                }
            }
            else
            {
                if (throwable.contactElapsed < m_LastContactElapsed && m_LastContactElapsed > 0.1f)
                    m_AudioSource.PlayOneShot(m_ContactAudioClip, 1.0f);
            }
            m_LastThrownElapsed = throwable.thrownElapsed;
            m_LastContactElapsed = throwable.contactElapsed;
        }

        public override bool IsVisible(EntityContainer entity)
        {
            var throwable = entity.Require<ThrowableComponent>();
            return throwable.thrownElapsed < throwable.popTime;
        }
    }
}