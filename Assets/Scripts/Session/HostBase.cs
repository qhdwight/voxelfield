using System;
using Components;
using Session.Components;
using Session.Player.Components;
using Util;

namespace Session
{
    public abstract class HostBase<TSessionComponent> : ServerBase<TSessionComponent>
        where TSessionComponent : SessionContainerBase
    {
        private const int HostPlayerId = 0;

        private readonly StandardPlayerCommandsContainer m_HostCommands = new StandardPlayerCommandsContainer();
        private readonly ContainerBase m_HostPlayerComponent;
        private readonly SessionContainerBase<StampedPlayerComponent> m_RenderSessionContainer;

        private float m_RenderTime;

        protected HostBase(IGameObjectLinker linker) : base(linker)
        {
            m_RenderSessionContainer = (SessionContainerBase<StampedPlayerComponent>) (object) Activator.CreateInstance<TSessionComponent>();
        }

        private void ReadLocalInputs(ContainerBase commandsToFill)
        {
            m_Modifier[HostPlayerId].ModifyCommands(commandsToFill);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs(m_HostCommands);
            m_HostCommands.duration.Value = delta;
            m_Modifier[HostPlayerId].ModifyTrusted(m_HostPlayerComponent, m_HostCommands);
            m_Modifier[HostPlayerId].ModifyChecked(m_HostPlayerComponent, m_HostCommands);
        }

        protected override void Render(float renderDelta, float timeSinceTick, float renderTime)
        {
            for (var playerId = 0; playerId < m_RenderSessionContainer.playerComponents.Length; playerId++)
            {
                if (playerId == HostPlayerId)
                {
                    // Inject host player component
                    m_RenderSessionContainer.localPlayerId.Value = HostPlayerId;
                    StandardPlayerContainer hostPlayerRenderComponent = m_RenderSessionContainer.playerComponents[HostPlayerId];
                    hostPlayerRenderComponent.MergeSet(m_HostPlayerComponent);
                }
                else
                {
                    float rollback = DebugBehavior.Singleton.Rollback * 2,
                          interpolatedTime = renderTime - rollback;
                    // Interpolate all remote players
                    for (var i = 0; i < m_SessionComponentHistory.Size; i++)
                    {
                        StampedPlayerComponent fromComponent = m_SessionComponentHistory.Get(-(i + 1)).playerComponents[playerId],
                                               toComponent = m_SessionComponentHistory.Get(-i).playerComponents[playerId];
                        if (fromComponent.stamp.time > interpolatedTime) continue;
                        float interpolation = (interpolatedTime - fromComponent.stamp.time) / (toComponent.stamp.time - fromComponent.stamp.time);
                        Interpolator.InterpolateInto(fromComponent, toComponent, m_RenderSessionContainer.playerComponents[playerId], interpolation);
                        break;
                    }
                    AnalysisLogger.AddDataPoint("", "V", m_RenderSessionContainer.playerComponents[playerId].position.Value.x);
                    // float timeSinceLastUpdate = renderTime - m_SessionComponentHistory.Peek().playerComponents[i].stamp.time;
                    // InterpolateHistoryInto(m_RenderSessionComponent.playerComponents[i],
                    //                        j => m_SessionComponentHistory.Get(j).playerComponents[i], m_SessionComponentHistory.Size,
                    //                        playerComponent => playerComponent.stamp.duration,
                    //                        DebugBehavior.Singleton.Rollback * 2, timeSinceLastUpdate);
                }
            }
            RenderSessionComponent(m_RenderSessionContainer);
        }

        protected override void PreTick(TSessionComponent tickSessionComponent)
        {
            // Inject our current player component before normal update cycle
            tickSessionComponent.playerComponents[HostPlayerId].MergeSet(m_HostPlayerComponent);
        }

        protected override void PostTick(TSessionComponent tickSessionComponent)
        {
            // Merge host component updates that happen on normal server update cycle
            m_HostPlayerComponent.MergeSet(tickSessionComponent.playerComponents[HostPlayerId]);
        }
    }
}