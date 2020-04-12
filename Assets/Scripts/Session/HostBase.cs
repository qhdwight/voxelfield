using System;
using System.Collections.Generic;
using System.Linq;
using Components;
using Session.Components;

namespace Session
{
    public abstract class HostBase : ServerBase
    {
        private const int HostPlayerId = 0;

        private readonly Container m_HostPlayerCommands, m_RenderSessionContainer;

        private float m_RenderTime;

        protected HostBase(IGameObjectLinker linker, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_HostPlayerCommands = new Container(commandElements.Concat(playerElements));
            m_RenderSessionContainer = new Container(sessionElements);
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty playerContainers))
                playerContainers.SetAll(() => new Container(playerElements));
        }

        private void ReadLocalInputs(Container commandsToFill)
        {
            m_Modifier[HostPlayerId].ModifyCommands(commandsToFill);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs(m_HostPlayerCommands);
            m_Modifier[HostPlayerId].ModifyTrusted(m_HostPlayerCommands, m_HostPlayerCommands, delta);
            m_Modifier[HostPlayerId].ModifyChecked(m_HostPlayerCommands, m_HostPlayerCommands, delta);
        }

        protected override void Render(float renderDelta, float timeSinceTick, float renderTime)
        {
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty renderPlayersProperty)
             && m_RenderSessionContainer.If(out LocalPlayerProperty localPlayerProperty))
            {
                for (var playerId = 0; playerId < renderPlayersProperty.Length; playerId++)
                {
                    if (localPlayerProperty.Value == HostPlayerId)
                    {
                        // Inject host player component
                        localPlayerProperty.Value = HostPlayerId;
                        renderPlayersProperty[HostPlayerId].MergeSet(m_HostPlayerCommands);
                    }
                    else
                    {
                        float rollback = DebugBehavior.Singleton.Rollback * 2,
                              interpolatedTime = renderTime - rollback;
                        // Interpolate all remote players
                        for (var i = 0; i < m_SessionComponentHistory.Size; i++)
                        {
                            Container fromComponent = m_SessionComponentHistory.Get(-(i + 1)).Require<PlayerContainerArrayProperty>()[playerId],
                                      toComponent = m_SessionComponentHistory.Get(-i).Require<PlayerContainerArrayProperty>()[playerId];
                            float toTime = toComponent.Require<ClientStampComponent>().time,
                                  fromTime = fromComponent.Require<ClientStampComponent>().time;
                            if (fromTime > interpolatedTime) continue;
                            float interpolation = (interpolatedTime - fromTime) / (toTime - fromTime);
                            Interpolator.InterpolateInto(fromComponent, toComponent, renderPlayersProperty[playerId], interpolation);
                            break;
                        }
                        // AnalysisLogger.AddDataPoint("", "V", m_RenderSessionContainer.playerComponents[playerId].position.Value.x);

                        // float timeSinceLastUpdate = renderTime - m_SessionComponentHistory.Peek().playerComponents[i].stamp.time;
                        // InterpolateHistoryInto(m_RenderSessionComponent.playerComponents[i],
                        //                        j => m_SessionComponentHistory.Get(j).playerComponents[i], m_SessionComponentHistory.Size,
                        //                        playerComponent => playerComponent.stamp.duration,
                        //                        DebugBehavior.Singleton.Rollback * 2, timeSinceLastUpdate);
                    }
                    m_Visuals[playerId].Render(renderPlayersProperty[playerId], playerId == localPlayerProperty);
                }
            }
        }

        protected override void PreTick(Container tickSessionComponent)
        {
            // Inject our current player component before normal update cycle
            tickSessionComponent.Require<PlayerContainerArrayProperty>()[HostPlayerId].MergeSet(m_HostPlayerCommands);
        }

        protected override void PostTick(Container tickSessionComponent)
        {
            // Merge host component updates that happen on normal server update cycle
            m_HostPlayerCommands.MergeSet(tickSessionComponent.Require<PlayerContainerArrayProperty>()[HostPlayerId]);
        }
    }
}