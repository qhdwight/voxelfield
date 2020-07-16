using System.Linq;
using System.Net;
using LiteNetLib;
using Steamworks;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Sessions.Player.Visualization;
using UnityEngine;
using UnityEngine.Profiling;

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
                                                   .Append(typeof(ServerTag))
                                                   .Append(typeof(HostTag)));
            SetFirstCommand(m_HostCommands);
        }

        public override Container GetLocalCommands() => m_HostCommands;

        protected override void Input(uint timeUs, uint deltaUs)
        {
            Container session = GetLatestSession();
            if (session.Without(out ServerStampComponent serverStamp) || serverStamp.tick.WithoutValue)
                return;

            if (!IsLoading)
            {
                PlayerModifierDispatcherBehavior hostModifier = GetPlayerModifier(GetModifyingPayerFromId(HostPlayerId, session), HostPlayerId);
                if (hostModifier)
                {
                    hostModifier.ModifyCommands(this, m_HostCommands, HostPlayerId);
                    _container = m_HostCommands; // Prevent closure allocation
                    _session = this;
                    ForEachSessionInterface(@interface => @interface.ModifyLocalTrusted(HostPlayerId, _session, _container));
                    hostModifier.ModifyTrusted(this, HostPlayerId, m_HostCommands, m_HostCommands, m_HostCommands, deltaUs);
                    hostModifier.ModifyChecked(this, HostPlayerId, m_HostCommands, m_HostCommands, deltaUs);
                }
                GetModifyingMode(session).ModifyPlayer(this, session, HostPlayerId, m_HostCommands, m_HostCommands, deltaUs);
            }
            var stamp = m_HostCommands.Require<ServerStampComponent>();
            stamp.timeUs.Value = timeUs;
            stamp.tick.Value = serverStamp.tick;
            Client.ClearSingleTicks(m_HostCommands);
        }

        protected override void Render(uint renderTimeUs)
        {
            Profiler.BeginSample("Host Render Setup");
            if (m_RenderSession.Without(out PlayerContainerArrayElement renderPlayers)
             || m_RenderSession.Without(out LocalPlayerId localPlayer)
             || IsLoading)
            {
                Profiler.EndSample();
                return;
            }

            var tickRate = GetLatestSession().Require<TickRateProperty>();
            if (!tickRate.WithValue) return;

            m_RenderSession.CopyFrom(GetLatestSession());
            localPlayer.Value = HostPlayerId;
            Profiler.EndSample();

            Profiler.BeginSample("Host Render Players");
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
                    uint playerRenderTimeUs = renderTimeUs - tickRate.PlayerRenderIntervalUs;
                    _indexer = playerId;
                    _serverHistory = m_SessionHistory;
                    RenderInterpolatedPlayer<ServerStampComponent>(playerRenderTimeUs, renderPlayer, m_SessionHistory.Size,
                                                                   historyIndex => _serverHistory.Get(-historyIndex).Require<PlayerContainerArrayElement>()[_indexer]);
                }
                PlayerVisualsDispatcherBehavior visuals = GetPlayerVisuals(renderPlayer, playerId);
                if (visuals) visuals.Render(this, m_RenderSession, playerId, renderPlayer, playerId == localPlayer);
            }
            Profiler.EndSample();

            Profiler.BeginSample("Host Render Interfaces");
            RenderInterfaces(m_RenderSession);
            Profiler.EndSample();

            Profiler.BeginSample("Host Render Entities");
            RenderEntities<ServerStampComponent>(renderTimeUs, tickRate.TickIntervalUs);
            Profiler.EndSample();

            Profiler.BeginSample("Host Render Mode");
            ModeManager.GetMode(m_RenderSession).Render(this, m_RenderSession);
            Profiler.EndSample();
        }

        protected override void PreTick(Container tickSession)
        {
            // Set up new player component data
            if (IsLoading) return;
            Container hostPlayer = tickSession.GetPlayer(HostPlayerId);
            if (hostPlayer.Require<HealthProperty>().WithValue)
            {
                // Inject our current player component before normal update cycle
                hostPlayer.MergeFrom(m_HostCommands);
            }
        }

        protected override void PostTick(Container tickSession)
        {
            if (IsLoading) return;
            Container hostPlayer = tickSession.GetPlayer(HostPlayerId);
            if (hostPlayer.Require<HealthProperty>().WithValue) return;
            // Set up new player component data
            SetupNewPlayer(tickSession, HostPlayerId, hostPlayer, tickSession);
            tickSession.Require<LocalPlayerId>().Value = HostPlayerId;
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

        protected override int GetPeerPlayerId(NetPeer peer) => base.GetPeerPlayerId(peer) + 1; // Reserve zero for host player

        // TODO:refactor bad
        public override Container GetModifyingPayerFromId(int playerId, Container session = null)
            => playerId == HostPlayerId ? m_HostCommands : base.GetModifyingPayerFromId(playerId, session);

        public override Ray GetRayForPlayerId(int playerId)
            => playerId == HostPlayerId ? GetRayForPlayer(m_HostCommands) : base.GetRayForPlayerId(playerId);
    }
}