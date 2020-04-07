using System;
using Components;
using Session.Items.Modifiers;
using Session.Player;
using Session.Player.Components;
using Session.Player.Modifiers;

namespace Session
{
    public abstract class HostBase<TSessionComponent>
        : ServerBase<TSessionComponent>
        where TSessionComponent : SessionComponentBase
    {
        private const int HostPlayerId = 0;

        private readonly PlayerCommandsComponent m_HostCommands = new PlayerCommandsComponent();
        private readonly PlayerComponent m_HostPlayerComponent = new PlayerComponent();

        private readonly TSessionComponent m_RenderSessionComponent = Activator.CreateInstance<TSessionComponent>();

        private float m_RenderTime;

        protected HostBase(IGameObjectLinker linker) : base(linker)
        {
        }

        private void ReadLocalInputs()
        {
           m_Modifier[HostPlayerId].ModifyCommands(m_HostCommands);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs();
            m_HostCommands.duration.Value = delta;
            m_Modifier[HostPlayerId].ModifyTrusted(m_HostPlayerComponent, m_HostCommands);
            m_Modifier[HostPlayerId].ModifyChecked(m_HostPlayerComponent, m_HostCommands);
        }

        protected override void Render(float renderDelta, float timeSinceTick)
        {
            // Interpolate all remote players
            InterpolateHistoryInto(m_RenderSessionComponent, m_SessionComponentHistory, session => session.stamp.duration, DebugBehavior.Singleton.Rollback, timeSinceTick);
            // Inject host player component
            m_RenderSessionComponent.localPlayerId.Value = HostPlayerId;
            PlayerComponent hostPlayerRenderComponent = m_RenderSessionComponent.playerComponents[HostPlayerId];
            Copier.MergeSet(hostPlayerRenderComponent, m_HostPlayerComponent);
            
            RenderSessionComponent(m_RenderSessionComponent);
        }

        protected override void Tick(uint tick, float time)
        {
            base.Tick(tick, time);
            TSessionComponent trustedSessionComponent = m_SessionComponentHistory.Peek();
            trustedSessionComponent.localPlayerId.Value = HostPlayerId;
            // Merge updates that happen on normal update cycle with host
            Copier.MergeSet(m_HostPlayerComponent, trustedSessionComponent.LocalPlayerComponent);
            Copier.MergeSet(trustedSessionComponent.LocalPlayerComponent, m_HostPlayerComponent);
        }
    }
}