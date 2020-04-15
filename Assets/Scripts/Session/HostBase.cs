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

        protected override void Render(float timeSinceTick, float renderTime)
        {
            if (m_RenderSessionContainer.If(out PlayerContainerArrayProperty renderPlayersProperty)
             && m_RenderSessionContainer.If(out LocalPlayerProperty localPlayerProperty))
            {
                localPlayerProperty.Value = HostPlayerId;
                for (var playerId = 0; playerId < renderPlayersProperty.Length; playerId++)
                {
                    if (playerId == localPlayerProperty)
                    {
                        // Inject host player component
                        renderPlayersProperty[playerId].MergeSet(m_HostPlayerCommands);
                    }
                    else
                    {
                        int copiedPlayerId = playerId;
                        Container GetInHistory(int historyIndex) => m_SessionComponentHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];
                        float rollback = DebugBehavior.Singleton.Rollback * 3;
                        RenderInterpolatedPlayer<ClientStampComponent>(renderTime - rollback, renderPlayersProperty[playerId], m_SessionComponentHistory.Size, GetInHistory);
                    }
                    m_Visuals[playerId].Render(renderPlayersProperty[playerId], playerId == localPlayerProperty);
                }
            }
        }

        protected override void PreTick(Container tickSessionContainer)
        {
            // Inject our current player component before normal update cycle
            tickSessionContainer.Require<PlayerContainerArrayProperty>()[HostPlayerId].MergeSet(m_HostPlayerCommands);
        }

        protected override void PostTick(Container tickSessionContainer)
        {
            // Merge host component updates that happen on normal server update cycle
            m_HostPlayerCommands.MergeSet(tickSessionContainer.Require<PlayerContainerArrayProperty>()[HostPlayerId]);
        }
    }
}