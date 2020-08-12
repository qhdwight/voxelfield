using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;

namespace Swihoni.Sessions
{
    public partial class Client
    {
        private readonly CyclicArray<Container> m_PlayerPredictionHistory;
        private readonly CyclicArray<ClientCommandsContainer> m_CommandHistory;
        
        public int PredictionErrors { get; private set; }

        private void Predict(uint tick, uint timeUs, int localPlayerId)
        {
            Container previousPredictedPlayer = m_PlayerPredictionHistory.Peek(),
                      predictedPlayer = m_PlayerPredictionHistory.ClaimNext();
            ClientCommandsContainer previousCommand = m_CommandHistory.Peek(),
                                    commands = m_CommandHistory.ClaimNext();
            if (predictedPlayer.Without(out ClientStampComponent predictedStamp)) return;

            predictedPlayer.SetTo(previousPredictedPlayer);
            commands.SetTo(previousCommand);

            if (IsLoading)
            {
                commands.Require<ClientStampComponent>().Clear();
            }
            else
            {
                predictedStamp.tick.Value = tick;
                predictedStamp.timeUs.Value = timeUs;
                var previousClientStamp = previousPredictedPlayer.Require<ClientStampComponent>();
                if (previousClientStamp.timeUs.WithValue)
                {
                    uint lastTime = previousClientStamp.timeUs.Else(timeUs),
                         durationUs = timeUs - lastTime;
                    predictedStamp.durationUs.Value = durationUs;
                }

                // Inject trusted component
                commands.Require<ClientStampComponent>().SetTo(predictedStamp);
                MergeCommandInto(predictedPlayer, commands);
                if (IsLoading || predictedStamp.durationUs.WithoutValue) return;

                PlayerModifierDispatcherBehavior modifier = GetPlayerModifier(predictedPlayer, localPlayerId);
                if (!modifier) return;

                var context = new SessionContext(this, GetLatestSession(), commands, localPlayerId, predictedPlayer,
                                                 timeUs: timeUs, durationUs: predictedStamp.durationUs, tickDelta: 1);
                modifier.ModifyChecked(context);
            }
        }

        private void CheckPrediction(Container serverPlayer, int localPlayerId)
        {
            UIntProperty targetClientTick = serverPlayer.Require<ClientStampComponent>().tick;
            if (targetClientTick.WithoutValue)
                return;

            var playerHistoryIndex = 0;
            Container basePredictedPlayer = null;
            for (; playerHistoryIndex < m_PlayerPredictionHistory.Size; playerHistoryIndex++)
            {
                Container predictedPlayer = m_PlayerPredictionHistory.Get(-playerHistoryIndex);
                if (predictedPlayer.Require<ClientStampComponent>().tick == targetClientTick)
                {
                    basePredictedPlayer = predictedPlayer;
                    break;
                }
            }
            if (basePredictedPlayer is null)
                return;

            /* We are checking predicted */
            Container latestPredictedPlayer = m_PlayerPredictionHistory.Peek();
            _predictionIsAccurate = true; // Set by the following navigation
            ElementExtensions.NavigateZipped(VisitPredictedFunction, basePredictedPlayer, latestPredictedPlayer, serverPlayer);
            if (_predictionIsAccurate)
                return;

            /* We did not predict properly */
            PredictionErrors++;
            // Place base from verified server
            basePredictedPlayer.SetTo(serverPlayer);
            // Replay old commands up until most recent to get back on track
            for (int commandHistoryIndex = playerHistoryIndex - 1; commandHistoryIndex >= 0; commandHistoryIndex--)
            {
                ClientCommandsContainer commands = m_CommandHistory.Get(-commandHistoryIndex);
                Container pastPredictedPlayer = m_PlayerPredictionHistory.Get(-commandHistoryIndex);
                ClientStampComponent stamp = pastPredictedPlayer.Require<ClientStampComponent>().Clone(); // TODO:performance remove clone
                pastPredictedPlayer.SetTo(m_PlayerPredictionHistory.Get(-commandHistoryIndex - 1));
                pastPredictedPlayer.Require<ClientStampComponent>().SetTo(stamp);
                PlayerModifierDispatcherBehavior localPlayerModifier = GetPlayerModifier(pastPredictedPlayer, localPlayerId);
                if (commands.Require<ClientStampComponent>().durationUs.WithValue)
                {
                    // TODO:architecture use latest session?
                    Container serverSession = GetLatestSession();
                    var context = new SessionContext(this, serverSession, commands, localPlayerId, pastPredictedPlayer,
                                                     durationUs: commands.Require<ClientStampComponent>().durationUs, tickDelta: 1);
                    localPlayerModifier.ModifyChecked(context);
                }
            }
        }

        private static Navigation VisitPredicted(ElementBase _predicted, ElementBase _latestPredicted, ElementBase _server)
        {
            if (_predicted.WithAttribute<OnlyServerTrustedAttribute>())
            {
                _latestPredicted.SetTo(_server);
                return Navigation.SkipDescendents;
            }
            if (_predicted.WithAttribute<ClientTrustedAttribute>() || _predicted.WithAttribute<ClientNonCheckedAttribute>()) return Navigation.SkipDescendents;
            switch (_predicted)
            {
                case FloatProperty f1 when _server is FloatProperty f2 && (f1.WithValue && f2.WithValue && f1.TryAttribute(out PredictionToleranceAttribute fTolerance)
                                                                        && !f1.CheckWithinTolerance(f2, fTolerance.tolerance)
                                                                        || f1.WithoutValue && f2.WithValue || f1.WithValue && f2.WithoutValue):
                case VectorProperty v1 when _server is VectorProperty v2 && (v1.WithValue && v2.WithValue && v1.TryAttribute(out PredictionToleranceAttribute vTolerance)
                                                                          && !v1.CheckWithinTolerance(v2, vTolerance.tolerance)
                                                                          || v1.WithoutValue && v2.WithValue || v1.WithValue && v2.WithoutValue):
                case PropertyBase p1 when _server is PropertyBase p2 && !p1.Equals(p2):
                    _predictionIsAccurate = false;
                    if (DefaultConfig.Active.logPredictionErrors)
                        Debug.LogWarning($"Error with predicted: {_predicted} and verified: {_server}");
                    return Navigation.Exit;
            }
            return Navigation.Continue;
        }
    }
}