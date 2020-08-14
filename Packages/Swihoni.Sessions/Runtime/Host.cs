using System;
using System.Linq;
using System.Net;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Sessions.Player.Visualization;
using Swihoni.Util;
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

        public override Container GetLocalPlayer() => m_HostCommands;

        protected override void Input(uint timeUs, uint durationUs)
        {
            Container session = GetLatestSession();
            if (session.Without(out ServerStampComponent serverStamp) || serverStamp.tick.WithoutValue)
                return;

            if (!IsLoading)
            {
                Container hostPlayer = GetModifyingPlayerFromId(HostPlayerId, session);
                if (hostPlayer.Health().WithValue)
                {
                    PlayerModifierDispatcherBehavior hostModifier = GetPlayerModifier(hostPlayer, HostPlayerId);
                    var hostContext = new SessionContext(this, session, m_HostCommands, HostPlayerId, m_HostCommands, durationUs: durationUs, tickDelta: 1);
                    try
                    {
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
                        hostContext.ModifyingMode.ModifyPlayer(hostContext);
                        m_Injector.ModifyPlayer(hostContext);
                    }
                    catch (Exception exception)
                    {
                        ExceptionLogger.Log(exception, "Exception modifying checked host");
                    }
                }
            }
            var stamp = m_HostCommands.Require<ServerStampComponent>();
            stamp.timeUs.Value = timeUs;
            stamp.tick.Value = serverStamp.tick;
            Client.ClearSingleTicks(m_HostCommands);
        }

        protected override void Render(uint renderTimeUs)
        {
            try
            {
                Profiler.BeginSample("Host Render Setup");
                if (IsLoading || m_RenderSession.Without(out PlayerArray renderPlayers)
                              || m_RenderSession.Without(out LocalPlayerId renderLocalPlayerId)
                              || m_HostCommands.Health().WithoutValue)
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
                                                                       historyIndex => _serverHistory.Get(-historyIndex).Require<PlayerArray>()[_indexer]);
                    }
                    PlayerVisualsDispatcherBehavior visuals = GetPlayerVisuals(renderPlayer, playerId);
                    bool isPossessed = isActualLocalPlayer && !isSpectating || isSpectating && playerId == spectatingPlayerId;
                    var playerContext = new SessionContext(this, m_RenderSession, playerId: playerId, player: renderPlayer);
                    if (visuals) visuals.Render(playerContext, isPossessed);
                }
                Profiler.EndSample();

                var context = new SessionContext(this, m_RenderSession, timeUs: renderTimeUs);

                Profiler.BeginSample("Host Render Interfaces");
                RenderInterfaces(context);
                Profiler.EndSample();

                Profiler.BeginSample("Host Render Entities");
                RenderEntities<ServerStampComponent>(renderTimeUs, tickRate.TickIntervalUs);
                Profiler.EndSample();

                Profiler.BeginSample("Host Render Mode");
                context.Mode.Render(context);
                Profiler.EndSample();
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception trying to render: {exception}");
            }
        }

        protected override void PreTick(Container tickSession)
        {
            base.PreTick(tickSession);
            if (IsLoading) return;
            Container hostPlayer = tickSession.GetPlayer(HostPlayerId);
            if (hostPlayer.Health().WithValue)
            {
                // Inject our current player component before normal update cycle
                hostPlayer.SetTo(m_HostCommands);
            }
        }

        protected override void PostTick(Container tickSession)
        {
            if (IsLoading) return;

            Container hostPlayer = tickSession.GetPlayer(HostPlayerId);
            if (hostPlayer.Health().WithoutValue)
            {
                var context = new SessionContext(this, tickSession, playerId: HostPlayerId, player: hostPlayer);
                m_Injector.OnSetupHost(context);
                SetupNewPlayer(context);
                tickSession.Require<LocalPlayerId>().Value = HostPlayerId;
                m_HostCommands.SetTo(hostPlayer);
            }
            base.PostTick(tickSession);

            RenderVerified(new SessionContext(this, tickSession));
        }

        protected override void RollbackHitboxes(in SessionContext context)
        {
            if (context.playerId == HostPlayerId)
            {
                for (var playerId = 0; playerId < MaxPlayers; playerId++)
                {
                    var visuals = (PlayerVisualsDispatcherBehavior) PlayerManager.UnsafeVisuals[playerId];
                    if (!visuals) continue;
                    Container player = visuals.GetRecentPlayer();
                    var playerContext = new SessionContext(existing: context, playerId: playerId, player: player);
                    GetPlayerModifier(player, playerId).EvaluateHitboxes(playerContext);
                }
            }
            else base.RollbackHitboxes(context);
        }

        public override int GetPeerPlayerId(NetPeer peer) => base.GetPeerPlayerId(peer) + 1; // Reserve zero for host player

        // TODO:refactor bad
        public override Container GetModifyingPlayerFromId(int playerId, Container session = null)
            => playerId == HostPlayerId ? m_HostCommands : base.GetModifyingPlayerFromId(playerId, session);

        public override Ray GetRayForPlayerId(int playerId)
            => playerId == HostPlayerId ? m_HostCommands.GetRayForPlayer() : base.GetRayForPlayerId(playerId);
    }
}