using System.Linq;
using System.Net;
using LiteNetLib;
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
            m_HostCommands = new Container(elements.playerElements
                                                   .Append(typeof(ClientStampComponent))
                                                   .Append(typeof(AcknowledgedServerTickProperty))
                                                   .Append(typeof(ServerStampComponent))
                                                   .Concat(elements.commandElements)
                                                   .Append(typeof(ServerTag))
                                                   .Append(typeof(HostTag)));
            injector.OnPlayerRegisterAppend(m_HostCommands);
            SetFirstCommand(m_HostCommands);
        }

        public override Container GetLocalCommands() => m_HostCommands;

        protected override void Input(uint timeUs, uint durationUs)
        {
            Container session = GetLatestSession();
            if (session.Without(out ServerStampComponent serverStamp) || serverStamp.tick.WithoutValue)
                return;

            if (!IsLoading)
            {
                Container hostPlayer = GetModifyingPayerFromId(HostPlayerId, session);
                if (hostPlayer.Require<HealthProperty>().WithValue)
                {
                    PlayerModifierDispatcherBehavior hostModifier = GetPlayerModifier(hostPlayer, HostPlayerId);
                    var hostContext = new SessionContext(this, session, m_HostCommands, HostPlayerId, m_HostCommands, durationUs: durationUs, tickDelta: 1);
                    if (hostModifier)
                    {
                        hostModifier.ModifyCommands(this, m_HostCommands, HostPlayerId);
                        _container = m_HostCommands; // Prevent closure allocation
                        _session = this;
                        ForEachSessionInterface(@interface => @interface.ModifyLocalTrusted(HostPlayerId, _session, _container));
                        // this, HostPlayerId, m_HostCommands, m_HostCommands, m_HostCommands, deltaUs
                        hostModifier.ModifyTrusted(hostContext, m_HostCommands);
                        hostModifier.ModifyChecked(hostContext);
                    }
                    GetModifyingMode(session).ModifyPlayer(hostContext);
                }
            }
            var stamp = m_HostCommands.Require<ServerStampComponent>();
            stamp.timeUs.Value = timeUs;
            stamp.tick.Value = serverStamp.tick;
            Client.ClearSingleTicks(m_HostCommands);
        }

        protected override void Render(uint renderTimeUs)
        {
            Profiler.BeginSample("Host Render Setup");
            if (IsLoading || m_RenderSession.Without(out PlayerContainerArrayElement renderPlayers)
                          || m_RenderSession.Without(out LocalPlayerId renderLocalPlayerId)
                          || m_HostCommands.Require<HealthProperty>().WithoutValue)
            {
                Profiler.EndSample();
                return;
            }

            var tickRate = GetLatestSession().Require<TickRateProperty>();
            if (!tickRate.WithValue) return;

            m_RenderSession.SetTo(GetLatestSession());
            Profiler.EndSample();

            Profiler.BeginSample("Host Spectate Setup");
            bool isSpectating = Client.IsSpectating(m_RenderSession, renderPlayers, HostPlayerId, out SpectatingPlayerId spectatingPlayerId);
            renderLocalPlayerId.Value = isSpectating ? spectatingPlayerId.Value : (byte) HostPlayerId;
            Profiler.EndSample();

            Profiler.BeginSample("Host Render Players");
            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                Container renderPlayer = renderPlayers[playerId];
                bool isActualLocalPlayer = playerId == HostPlayerId;
                if (isActualLocalPlayer)
                {
                    // Inject host player component
                    renderPlayer.SetTo(m_HostCommands);
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
                bool isPossessed = isActualLocalPlayer && !isSpectating || isSpectating && playerId == spectatingPlayerId;
                if (visuals) visuals.Render(this, m_RenderSession, playerId, renderPlayer, isPossessed);
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
            if (hostPlayer.Require<HealthProperty>().WithoutValue)
            {
                var context = new SessionContext(this, tickSession, playerId: HostPlayerId, player: hostPlayer);
                m_Injector.OnSetupHost(context);
                SetupNewPlayer(context);
                tickSession.Require<LocalPlayerId>().Value = HostPlayerId;
                m_HostCommands.SetTo(hostPlayer);
            }
            base.PostTick(tickSession);
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

        public override int GetPeerPlayerId(NetPeer peer) => base.GetPeerPlayerId(peer) + 1; // Reserve zero for host player

        // TODO:refactor bad
        public override Container GetModifyingPayerFromId(int playerId, Container session = null)
            => playerId == HostPlayerId ? m_HostCommands : base.GetModifyingPayerFromId(playerId, session);

        public override Ray GetRayForPlayerId(int playerId)
            => playerId == HostPlayerId ? m_HostCommands.GetRayForPlayer() : base.GetRayForPlayerId(playerId);
    }
}