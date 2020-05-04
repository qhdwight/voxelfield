using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Swihoni.Sessions
{
    public abstract class HostBase : ServerBase
    {
        private const int HostPlayerId = 0;

        private readonly Container m_HostCommands;

        protected HostBase(ISessionGameObjectLinker linker,
                           IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
            : base(linker, sessionElements, playerElements, commandElements)
        {
            // TODO:refactor zeroing
            m_HostCommands = new Container(playerElements
                                          .Append(typeof(ClientStampComponent))
                                          .Append(typeof(ServerStampComponent))
                                          .Concat(commandElements)
                                          .Append(typeof(ServerTag)));
            m_HostCommands.Zero();
            m_HostCommands.Require<ServerStampComponent>().Reset();
        }

        private void ReadLocalInputs(Container commandsToFill) => m_Modifier[HostPlayerId].ModifyCommands(this, commandsToFill);

        protected override void Input(float time, float delta)
        {
            Container session = GetLatestSession();
            if (session.Without(out ServerStampComponent serverStamp) || serverStamp.tick.WithoutValue)
                return;

            ReadLocalInputs(m_HostCommands);
            // m_HostCommands.Require<InventoryComponent>().Zero();
            m_Modifier[HostPlayerId].ModifyTrusted(this, HostPlayerId, m_HostCommands, m_HostCommands, delta);
            m_Modifier[HostPlayerId].ModifyChecked(this, HostPlayerId, m_HostCommands, m_HostCommands, delta);
            GetMode(session).Modify(session, m_HostCommands, m_HostCommands, delta);
            var stamp = m_HostCommands.Require<ServerStampComponent>();
            stamp.time.Value = time;
            stamp.tick.Value = serverStamp.tick;
        }

        protected override void Render(float renderTime)
        {
            base.Render(renderTime);
            if (m_RenderSession.Without(out PlayerContainerArrayProperty renderPlayers)
             || m_RenderSession.Without(out LocalPlayerProperty localPlayer)) return;

            localPlayer.Value = HostPlayerId;
            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                if (playerId == localPlayer)
                {
                    // Inject host player component
                    renderPlayers[playerId].FastCopyFrom(m_HostCommands);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayProperty>()[copiedPlayerId];

                    SessionSettingsComponent settings = GetSettings();
                    if (!settings.tickRate.HasValue) break;
                    float rollback = DebugBehavior.Singleton.RollbackOverride.OrElse(settings.TickInterval) * 3;

                    RenderInterpolatedPlayer<ServerStampComponent>(renderTime - rollback, renderPlayers[playerId],
                                                                   m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(playerId, renderPlayers[playerId], playerId == localPlayer);
            }
            m_PlayerHud.Render(renderPlayers[HostPlayerId]);
        }

        protected override void PreTick(Container tickSession)
        {
            Container hostPlayer = tickSession.GetPlayer(HostPlayerId);
            // Inject our current player component before normal update cycle
            hostPlayer.FastMergeSet(m_HostCommands);
            // Set up new player component data
            if (tickSession.Require<ServerStampComponent>().tick == 0u) SetupNewPlayer(tickSession, hostPlayer);
        }

        protected override void RollbackHitboxes(int playerId)
        {
            if (playerId == HostPlayerId)
            {
                for (var i = 0; i < m_Modifier.Length; i++)
                    m_Modifier[i].EvaluateHitboxes(i, m_Visuals[i].GetRecentPlayer());
            }
            else
                base.RollbackHitboxes(playerId);
        }

        // TODO:refactor bad
        public override Container GetPlayerFromId(int playerId) => playerId == HostPlayerId ? m_HostCommands : base.GetPlayerFromId(playerId);

        public override Ray GetRayForPlayerId(int playerId) => playerId == HostPlayerId ? GetRayForPlayer(m_HostCommands) : base.GetRayForPlayerId(playerId);
    }
}