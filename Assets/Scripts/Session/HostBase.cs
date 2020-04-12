using System;
using System.Collections.Generic;
using Components;
using Session.Components;

namespace Session
{
    public abstract class HostBase : ServerBase
    {
        private const int HostPlayerId = 0;

        private readonly Container m_HostPlayerCommands, m_HostPlayerComponent;
        private readonly SessionContainer m_RenderSessionContainer;

        private float m_RenderTime;

        protected HostBase(IGameObjectLinker linker, List<Type> sessionElements, List<Type> playerElements, List<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_HostPlayerCommands = (Container) Activator.CreateInstance(commandsType);
            m_HostPlayerComponent = (Container) Activator.CreateInstance(playerType);
            m_RenderSessionContainer = (SessionContainer) Activator.CreateInstance(sessionType);
            for (var i = 0; i < m_RenderSessionContainer.playerComponents.Length; i++)
                m_RenderSessionContainer.playerComponents[i] = new StampedPlayerComponent {player = (Container) Activator.CreateInstance(playerType)};
        }

        private void ReadLocalInputs(Container commandsToFill)
        {
            m_Modifier[HostPlayerId].ModifyCommands(commandsToFill);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs(m_HostPlayerCommands);
            m_Modifier[HostPlayerId].ModifyTrusted(m_HostPlayerComponent, m_HostPlayerCommands, delta);
            m_Modifier[HostPlayerId].ModifyChecked(m_HostPlayerComponent, m_HostPlayerCommands, delta);
        }

        protected override void Render(float renderDelta, float timeSinceTick, float renderTime)
        {
            for (var playerId = 0; playerId < m_RenderSessionContainer.playerComponents.Length; playerId++)
            {
                if (playerId == HostPlayerId)
                {
                    // Inject host player component
                    m_RenderSessionContainer.localPlayerId.Value = HostPlayerId;
                    m_RenderSessionContainer.playerComponents[HostPlayerId].player.MergeSet(m_HostPlayerComponent);
                }
                else
                {
                    float rollback = DebugBehavior.Singleton.Rollback * 2,
                          interpolatedTime = renderTime - rollback;
                    // Interpolate all remote players
                    for (var i = 0; i < m_SessionComponentHistory.Size; i++)
                    {
                        ServerPlayerContainer fromComponent = m_SessionComponentHistory.Get(-(i + 1)).playerComponents[playerId],
                                              toComponent = m_SessionComponentHistory.Get(-i).playerComponents[playerId];
                        if (fromComponent.clientStamp.time > interpolatedTime) continue;
                        float interpolation = (interpolatedTime - fromComponent.clientStamp.time) / (toComponent.clientStamp.time - fromComponent.clientStamp.time);
                        Interpolator.InterpolateInto(fromComponent, toComponent, m_RenderSessionContainer.playerComponents[playerId].player, interpolation);
                        break;
                    }
                    // AnalysisLogger.AddDataPoint("", "V", m_RenderSessionContainer.playerComponents[playerId].position.Value.x);

                    // float timeSinceLastUpdate = renderTime - m_SessionComponentHistory.Peek().playerComponents[i].stamp.time;
                    // InterpolateHistoryInto(m_RenderSessionComponent.playerComponents[i],
                    //                        j => m_SessionComponentHistory.Get(j).playerComponents[i], m_SessionComponentHistory.Size,
                    //                        playerComponent => playerComponent.stamp.duration,
                    //                        DebugBehavior.Singleton.Rollback * 2, timeSinceLastUpdate);
                }
            }
            for (var playerId = 0; playerId < m_RenderSessionContainer.playerComponents.Length; playerId++)
                m_Visuals[playerId].Render(m_RenderSessionContainer.playerComponents[playerId].player, playerId == m_RenderSessionContainer.localPlayerId);
        }

        protected override void PreTick(SessionContainerBase<ServerPlayerContainer> tickSessionComponent)
        {
            // Inject our current player component before normal update cycle
            tickSessionComponent.playerComponents[HostPlayerId].MergeSet(m_HostPlayerComponent);
        }

        protected override void PostTick(SessionContainerBase<ServerPlayerContainer> tickSessionComponent)
        {
            // Merge host component updates that happen on normal server update cycle
            m_HostPlayerComponent.MergeSet(tickSessionComponent.playerComponents[HostPlayerId]);
        }
    }
}