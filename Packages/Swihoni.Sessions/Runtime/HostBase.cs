using System.Linq;
using System.Net;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions
{
    public abstract class HostBase : ServerBase
    {
        private const int HostPlayerId = 0;

        private readonly Container m_HostCommands;

        protected HostBase(SessionElements elements, IPEndPoint ipEndPoint, EventBasedNetListener.OnConnectionRequest connectionRequestHandler)
            : base(elements, ipEndPoint, connectionRequestHandler)
        {
            // TODO:refactor zeroing
            m_HostCommands = new Container(elements.playerElements
                                                   .Append(typeof(ClientStampComponent))
                                                   .Append(typeof(AcknowledgedServerTickProperty))
                                                   .Append(typeof(ServerStampComponent))
                                                   .Concat(elements.commandElements)
                                                   .Append(typeof(ServerTag)));
            m_HostCommands.Zero();
            m_HostCommands.Require<ServerStampComponent>().Reset();
        }

        private void ReadLocalInputs(Container commandsToFill) => m_Modifier[HostPlayerId].ModifyCommands(this, commandsToFill);

        protected override void Input(uint timeUs, uint deltaUs)
        {
            Container session = GetLatestSession();
            if (session.Without(out ServerStampComponent serverStamp) || serverStamp.tick.WithoutValue)
                return;

            ReadLocalInputs(m_HostCommands);
            // m_HostCommands.Require<InventoryComponent>().Zero();
            m_Modifier[HostPlayerId].ModifyTrusted(this, HostPlayerId, m_HostCommands, m_HostCommands, deltaUs);
            m_Modifier[HostPlayerId].ModifyChecked(this, HostPlayerId, m_HostCommands, m_HostCommands, deltaUs);
            GetMode(session).Modify(session, m_HostCommands, m_HostCommands, deltaUs);
            var stamp = m_HostCommands.Require<ServerStampComponent>();
            stamp.timeUs.Value = timeUs;
            stamp.tick.Value = serverStamp.tick;
        }

        protected override void Render(float renderTime)
        {
            if (m_RenderSession.Without(out PlayerContainerArrayElement renderPlayers)
             || m_RenderSession.Without(out LocalPlayerProperty localPlayer)) return;

            var tickRate = GetLatestSession().Require<TickRateProperty>();
            if (!tickRate.WithValue) return;

            localPlayer.Value = HostPlayerId;
            base.Render(renderTime);

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
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayElement>()[copiedPlayerId];

                    uint renderTimeUs = TimeConversions.GetUsFromSecond(renderTime - tickRate.TickInterval * 4);
                    RenderInterpolatedPlayer<ServerStampComponent>(renderTimeUs, renderPlayers[playerId], m_SessionHistory.Size, GetInHistory);
                }
                m_Visuals[playerId].Render(playerId, renderPlayers[playerId], playerId == localPlayer);
            }
            m_PlayerHud.Render(renderPlayers[HostPlayerId]);
            RenderEntities<ServerStampComponent>(renderTime, tickRate.TickInterval);
        }

        protected override void PreTick(Container tickSession)
        {
            Container hostPlayer = tickSession.GetPlayer(HostPlayerId);
            // Inject our current player component before normal update cycle
            hostPlayer.MergeFrom(m_HostCommands);
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