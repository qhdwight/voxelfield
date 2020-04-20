using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;

namespace Swihoni.Sessions
{
    public abstract class HostBase : ServerBase
    {
        private const int HostPlayerId = 0;

        private readonly Container m_HostCommands, m_RenderSession;

        protected HostBase(IGameObjectLinker linker, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_HostCommands = new Container(commandElements.Concat(playerElements).Append(typeof(ClientStampComponent)));
            m_RenderSession = new Container(sessionElements);
            if (m_RenderSession.If(out PlayerContainerArrayProperty players))
                players.SetAll(() => new Container(playerElements));
        }

        private void ReadLocalInputs(Container commandsToFill)
        {
            m_Modifier[HostPlayerId].ModifyCommands(commandsToFill);
        }

        public override void Input(float time, float delta)
        {
            ReadLocalInputs(m_HostCommands);
            m_Modifier[HostPlayerId].ModifyTrusted(m_HostCommands, m_HostCommands, delta);
            m_Modifier[HostPlayerId].ModifyChecked(m_HostCommands, m_HostCommands, delta);
            var clientStamp = m_HostCommands.Require<ClientStampComponent>();
            clientStamp.duration.Value += delta;
            clientStamp.time.Value = time;
            clientStamp.tick.Value = m_SessionHistory.Peek().Require<ServerStampComponent>().tick;
        }

        protected override void Render(float renderTime, float tickElapsed)
        {
            if (!m_RenderSession.If(out PlayerContainerArrayProperty renderPlayers)
             || !m_RenderSession.If(out LocalPlayerProperty localPlayer)) return;

            localPlayer.Value = HostPlayerId;
            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                if (playerId == localPlayer)
                {
                    // Inject host player component
                    renderPlayers[playerId].CopyFrom(m_HostCommands);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    float rollback = DebugBehavior.Singleton.Rollback * 3;

                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];

                    float elapsed = renderTime - GetInHistory(0).Require<ServerStampComponent>().time;
                    float trackedClientTime = GetInHistory(0).Require<SimulatedTimeProperty>();

                    RenderInterpolatedPlayer<ClientStampComponent>(trackedClientTime + elapsed - rollback, renderPlayers[playerId],
                                                                   m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(renderPlayers[playerId], playerId == localPlayer);
            }
        }

        protected override void PreTick(Container tickSession)
        {
            // Inject our current player component before normal update cycle
            tickSession.Require<PlayerContainerArrayProperty>()[HostPlayerId].MergeSet(m_HostCommands);
        }

        protected override void PostTick(Container tickSession)
        {
            // Merge host component updates that happen on normal server update cycle
            m_HostCommands.MergeSet(tickSession.Require<PlayerContainerArrayProperty>()[HostPlayerId]);
        }
    }
}