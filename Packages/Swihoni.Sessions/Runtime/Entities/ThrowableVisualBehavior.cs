using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class ThrowableVisualBehavior : EntityVisualBehavior
    {
        private ParticleSystem[] m_Particles;
        private ThrowableModifierBehavior m_Modifier;

        internal override void Setup(EntityManager manager)
        {
            base.Setup(manager);
            m_Particles = GetComponentsInChildren<ParticleSystem>();
            m_Modifier = (ThrowableModifierBehavior) m_Manager.GetModifierPrefab(id);
            foreach (ParticleSystem particle in m_Particles) particle.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        public override void Render(EntityContainer entity)
        {
            base.Render(entity);

            var throwable = entity.Require<ThrowableComponent>();

            if (throwable.elapsed < m_Modifier.PopTime) return;

            float particleElapsed = throwable.elapsed - m_Modifier.PopTime;
            foreach (ParticleSystem particle in m_Particles)
            {
                particle.time = particleElapsed;
                if (particle.isStopped) particle.Play(false);
                // particle.Simulate(particleElapsed, false, true, false);
            }
        }

        public override bool IsVisible(EntityContainer entity) => entity.Require<ThrowableComponent>().elapsed < m_Modifier.PopTime;
    }
}