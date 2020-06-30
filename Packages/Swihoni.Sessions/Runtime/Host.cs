using System.Linq;
using System.Net;
using Steamworks;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Sessions.Player.Visualization;
using UnityEngine;

namespace Swihoni.Sessions
{
    public class Host : Server
    {
        private const int HostPlayerId = 0;

        private readonly Container m_HostCommands;

        public Host(SessionElements elements, IPEndPoint ipEndPoint, SessionInjectorBase injector)
            : base(elements, ipEndPoint, injector)
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

        protected override void Input(uint timeUs, uint deltaUs)
        {
            Container session = GetLatestSession();
            if (session.Without(out ServerStampComponent serverStamp) || serverStamp.tick.WithoutValue)
                return;

            if (!IsPaused)
            {
                PlayerModifierDispatcherBehavior hostModifier = GetPlayerModifier(GetPlayerFromId(HostPlayerId, session), HostPlayerId);
                if (hostModifier)
                {
                    hostModifier.ModifyCommands(this, m_HostCommands);
                    ForEachSessionInterface(@interface => @interface.ModifyLocalTrusted(HostPlayerId, this, m_HostCommands));
                    hostModifier.ModifyTrusted(this, HostPlayerId, m_HostCommands, m_HostCommands, m_HostCommands, deltaUs);
                    hostModifier.ModifyChecked(this, HostPlayerId, m_HostCommands, m_HostCommands, deltaUs);
                }
                GetMode(session).ModifyPlayer(this, session, HostPlayerId, m_HostCommands, m_HostCommands, deltaUs);
            }
            var stamp = m_HostCommands.Require<ServerStampComponent>();
            stamp.timeUs.Value = timeUs;
            stamp.tick.Value = serverStamp.tick;
            Client.ClearSingleTicks(m_HostCommands);
        }

        protected override void Render(uint renderTimeUs)
        {
            if (m_RenderSession.Without(out PlayerContainerArrayElement renderPlayers)
             || m_RenderSession.Without(out LocalPlayerId localPlayer)) return;

            var tickRate = GetLatestSession().Require<TickRateProperty>();
            if (!tickRate.WithValue) return;

            m_RenderSession.CopyFrom(GetLatestSession());
            localPlayer.Value = HostPlayerId;

            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                Container renderPlayer = renderPlayers[playerId];
                if (playerId == localPlayer)
                {
                    // Inject host player component
                    renderPlayer.CopyFrom(m_HostCommands);
                }
                else
                {
                    int copiedPlayerId = playerId;
                    Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).Require<PlayerContainerArrayElement>()[copiedPlayerId];

                    uint playerRenderTimeUs = renderTimeUs - tickRate.PlayerRenderIntervalUs;
                    RenderInterpolatedPlayer<ServerStampComponent>(playerRenderTimeUs, renderPlayer, m_SessionHistory.Size, GetInHistory);
                }
                PlayerVisualsDispatcherBehavior visuals = GetPlayerVisuals(renderPlayer, playerId);
                if (visuals) visuals.Render(this, m_RenderSession, playerId, renderPlayer, playerId == localPlayer);
            }
            RenderInterfaces(m_RenderSession);
            RenderEntities<ServerStampComponent>(renderTimeUs, tickRate.TickIntervalUs);
            GetMode(m_RenderSession).Render(m_RenderSession);
        }

        protected override void PreTick(Container tickSession)
        {
            // Set up new player component data
            if (IsPaused) return;
            Container hostPlayer = tickSession.GetPlayer(HostPlayerId);
            if (hostPlayer.Require<HealthProperty>().WithValue)
            {
                // Inject our current player component before normal update cycle
                hostPlayer.MergeFrom(m_HostCommands);
            }
        }

        protected override void PostTick(Container tickSession)
        {
            if (IsPaused) return;
            Container hostPlayer = tickSession.GetPlayer(HostPlayerId);
            if (hostPlayer.Require<HealthProperty>().WithValue) return;
            // Set up new player component data
            SetupNewPlayer(tickSession, hostPlayer);
            m_HostCommands.CopyFrom(hostPlayer);
            m_HostCommands.Require<UsernameProperty>().SetTo(SteamClient.IsValid ? SteamClient.Name : "Host");
        }

        protected override void RollbackHitboxes(int playerId)
        {
            if (playerId == HostPlayerId)
            {
                for (var i = 0; i < MaxPlayers; i++)
                {
                    var visuals = (PlayerVisualsDispatcherBehavior) PlayerManager.UnsafeVisuals[i];
                    if (!visuals) continue;
                    Container player = visuals.GetRecentPlayer();
                    GetPlayerModifier(player, i).EvaluateHitboxes(this, i, player);
                }
            }
            else base.RollbackHitboxes(playerId);
        }

        // TODO:refactor bad
        public override Container GetPlayerFromId(int playerId, Container session = null) => playerId == HostPlayerId ? m_HostCommands : base.GetPlayerFromId(playerId, session);

        public override Ray GetRayForPlayerId(int playerId) => playerId == HostPlayerId ? GetRayForPlayer(m_HostCommands) : base.GetRayForPlayerId(playerId);
    }
}