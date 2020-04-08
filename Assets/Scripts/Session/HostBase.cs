using System;
using Components;
using Session.Player;
using Session.Player.Components;

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
            hostPlayerRenderComponent.MergeSet(m_HostPlayerComponent);

            RenderSessionComponent(m_RenderSessionComponent);
        }

        protected override void PreTick(TSessionComponent tickSessionComponent)
        {
            // Inject our current player component before normal update cycle
            tickSessionComponent.localPlayerId.Value = HostPlayerId;
            tickSessionComponent.LocalPlayerComponent.MergeSet(m_HostPlayerComponent);
        }

        protected override void PostTick(TSessionComponent tickSessionComponent)
        {
            // Merge host component updates that happen on normal server update cycle
            m_HostPlayerComponent.MergeSet(tickSessionComponent.LocalPlayerComponent);
        }
    }
}