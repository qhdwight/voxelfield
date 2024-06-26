using System;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Visualization;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Swihoni.Sessions
{
    public partial class Client
    {
        private static readonly ProfilerMarker ClientRenderPlayers = new("Client Render Players"),
                                               ClientRenderInterfaces = new("Client Render Interfaces"),
                                               ClientRenderEntities = new("Client Render Entities"),
                                               ClientRenderMode = new("Client Render Mode");

        protected override void Render(uint renderTimeUs)
        {
            try
            {
                Profiler.BeginSample("Client Render Setup");
                if (IsLoading || m_RenderSession.Without(out PlayerArray renderPlayers)
                              || m_RenderSession.Without(out LocalPlayerId renderLocalPlayerId)
                              || !GetLocalPlayerId(GetLatestSession(), out int actualLocalPlayerId))
                {
                    Profiler.EndSample();
                    return;
                }

                var tickRate = GetLatestSession().Require<TickRateProperty>();
                if (tickRate.WithoutValue) return;

                m_RenderSession.SetTo(GetLatestSession());
                Profiler.EndSample();

                bool isSpectating = IsSpectating(m_RenderSession, renderPlayers, actualLocalPlayerId, out SpectatingPlayerId spectatingPlayerId);
                renderLocalPlayerId.Value = isSpectating ? spectatingPlayerId.Value : (byte) actualLocalPlayerId;

                using (ClientRenderPlayers.Auto())
                    RenderPlayers(renderTimeUs, renderPlayers, actualLocalPlayerId, tickRate, isSpectating, spectatingPlayerId);

                var context = new SessionContext(this, m_RenderSession, timeUs: renderTimeUs);

                using (ClientRenderInterfaces.Auto())
                    RenderInterfaces(context);

                using (ClientRenderEntities.Auto())
                    RenderEntities<LocalizedClientStampComponent>(renderTimeUs, tickRate.TickIntervalUs * 2u);

                using (ClientRenderMode.Auto())
                    context.Mode.Render(context);
            }
            catch (Exception exception)
            {
                Debug.LogError($"Exception trying to render: {exception}");
            }
        }

        private void RenderPlayers(uint renderTimeUs, PlayerArray renderPlayers, int actualLocalPlayerId, TickRateProperty tickRate, bool isSpectating,
                                   SpectatingPlayerId spectatingPlayerId)
        {
            for (var playerId = 0; playerId < renderPlayers.Length; playerId++)
            {
                bool isActualLocalPlayer = playerId == actualLocalPlayerId;
                Container renderPlayer = renderPlayers[playerId];
                if (isActualLocalPlayer)
                {
                    uint playerRenderTimeUs = renderTimeUs - tickRate.TickIntervalUs;
                    _predictionHistory = m_PlayerPredictionHistory;
                    RenderInterpolatedPlayer<ClientStampComponent>(playerRenderTimeUs, renderPlayer, m_PlayerPredictionHistory.Size,
                                                                   historyIndex => _predictionHistory.Get(-historyIndex));
                    MergeCommandInto(renderPlayer, m_CommandHistory.Peek());
                    // localPlayerRenderComponent.MergeSet(DebugBehavior.Singleton.RenderOverride);
                }
                else
                {
                    uint playerRenderTimeUs = renderTimeUs - tickRate.PlayerRenderIntervalUs;
                    _serverHistory = m_SessionHistory;
                    _indexer = playerId;
                    RenderInterpolatedPlayer<LocalizedClientStampComponent>(playerRenderTimeUs, renderPlayer, m_SessionHistory.Size,
                                                                            historyIndex =>
                                                                                _serverHistory.Get(-historyIndex).Require<PlayerArray>()[_indexer]);
                }
                PlayerVisualsDispatcherBehavior visuals = GetPlayerVisuals(renderPlayer, playerId);
                bool isPossessed = isActualLocalPlayer && !isSpectating || isSpectating && playerId == spectatingPlayerId;
                var playerContext = new SessionContext(this, m_RenderSession, playerId: playerId, player: renderPlayer);
                if (visuals) visuals.Render(playerContext, isPossessed);
            }
        }

        internal static bool IsSpectating(Container renderSession, PlayerArray renderPlayers, int actualLocalPlayerId, out SpectatingPlayerId spectatingPlayerId)
        {
            bool isSpectating = ModeManager.GetMode(renderSession).IsSpectating(renderSession, renderPlayers[actualLocalPlayerId]);
            spectatingPlayerId = renderSession.Require<SpectatingPlayerId>();
            if (isSpectating)
            {
                Container localPlayer = renderPlayers[actualLocalPlayerId];
                bool CanSpectate(int index)
                {
                    if (index == actualLocalPlayerId) return false;
                    Container spectateCandidate = renderPlayers[index];
                    return spectateCandidate.Health().IsActiveAndAlive && spectateCandidate.Require<TeamProperty>() == localPlayer.Require<TeamProperty>();
                }
                // Clear if currently spectated player can no longer be spectated
                if (spectatingPlayerId.WithValue)
                {
                    if (!CanSpectate(spectatingPlayerId))
                        spectatingPlayerId.Clear();
                }
                // Determine open player to spectate
                if (spectatingPlayerId.WithoutValue)
                {
                    for (byte playerId = 0; playerId < renderPlayers.Length; playerId++)
                    {
                        if (CanSpectate(playerId))
                        {
                            spectatingPlayerId.Value = playerId;
                            break;
                        }
                    }
                }
                // Handle switching
                if (spectatingPlayerId.WithValue)
                {
                    byte Wrap(int index)
                    {
                        while (index >= MaxPlayers) index -= MaxPlayers;
                        while (index < 0) index += MaxPlayers;
                        return (byte) index;
                    }
                    if (InputProvider.GetInputDown(InputType.NextSpectating))
                    {
                        for (int i = spectatingPlayerId + 1; i < spectatingPlayerId + MaxPlayers; i++)
                            if (CanSpectate(Wrap(i)))
                            {
                                spectatingPlayerId.Value = Wrap(i);
                                break;
                            }
                    }
                    else if (InputProvider.GetInputDown(InputType.PreviousSpectating))
                    {
                        for (int i = spectatingPlayerId - 1; i > spectatingPlayerId - MaxPlayers; i--)
                            if (CanSpectate(Wrap(i)))
                            {
                                spectatingPlayerId.Value = Wrap(i);
                                break;
                            }
                    }
                }
                else isSpectating = false;
            }
            if (!isSpectating) spectatingPlayerId.Clear();
            return isSpectating;
        }
    }
}