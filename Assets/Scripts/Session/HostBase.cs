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

        private readonly Container m_HostCommands, m_RenderSession;

        protected HostBase(IGameObjectLinker linker, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            m_HostCommands = new Container(commandElements.Concat(playerElements));
            m_RenderSession = new Container(sessionElements);
            if (m_RenderSession.If(out PlayerContainerArrayProperty players))
                players.SetAll(() => new Container(playerElements));
        }

        private void ReadLocalInputs(Container commandsToFill)
        {
            m_Modifier[HostPlayerId].ModifyCommands(commandsToFill);
        }

        public override void Input(float delta)
        {
            ReadLocalInputs(m_HostCommands);
            m_Modifier[HostPlayerId].ModifyTrusted(m_HostCommands, m_HostCommands, delta);
            m_Modifier[HostPlayerId].ModifyChecked(m_HostCommands, m_HostCommands, delta);
        }

        protected override void Render(float timeSinceTick, float renderTime)
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
                    Container GetInHistory(int historyIndex) => m_SessionComponentHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];
                    float rollback = DebugBehavior.Singleton.Rollback * 3;
                    RenderInterpolatedPlayer<ClientStampComponent>(renderTime - rollback, renderPlayers[playerId], m_SessionComponentHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(renderPlayers[playerId], playerId == localPlayer);
            }
        }

        protected override void PreTick(Container tickSession)
        {
            // Inject our current player component before normal update cycle
            Container hostPlayer = tickSession.Require<PlayerContainerArrayProperty>()[HostPlayerId];
            StampComponent hostClientStamp = hostPlayer.Require<ClientStampComponent>();
            StampComponent serverStamp = tickSession.Require<ServerStampComponent>();
            hostClientStamp.CopyFrom(serverStamp);
            hostPlayer.MergeSet(m_HostCommands);
            
        }

        protected override void PostTick(Container tickSession)
        {
            // Merge host component updates that happen on normal server update cycle
            m_HostCommands.MergeSet(tickSession.Require<PlayerContainerArrayProperty>()[HostPlayerId]);
        }
    }
}