using System;
using System.IO;
using System.Linq;
using System.Net;
using LiteNetLib;
using LiteNetLib.Utils;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Components.Networking;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Visualization;
using Swihoni.Util;
using Unity.Profiling;
using UnityEngine;
using UnityEngine.Profiling;

namespace Swihoni.Sessions
{
    public sealed partial class Client : NetworkedSessionBase, IReceiver
    {
        private const int DemoWriteSize = 1 << 18, DemoTickRate = 20;
        private ComponentClientSocket m_Socket;
        private readonly NetDataWriter m_DemoWriter = new NetDataWriter(true, DemoWriteSize);
        private static readonly ProfilerMarker ClientPredictMarker = new ProfilerMarker("Client Prediction");

        public override ComponentSocketBase Socket => m_Socket;
        public bool WriteDemo { get; set; }

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            ConsoleCommandExecutor.SetCommand("start_demo", args => SessionEnumerable.OfType<Client>().First().WriteDemo = true);
            ConsoleCommandExecutor.SetCommand("end_demo", args => SessionEnumerable.OfType<Client>().First().WriteDemo = false);
        }

        public Client(SessionElements elements, IPEndPoint ipEndPoint, SessionInjectorBase injector)
            : base(elements, ipEndPoint, injector)
        {
            /* Prediction */
            m_CommandHistory = new CyclicArray<ClientCommandsContainer>(HistoryCount, () => m_EmptyClientCommands.Clone());
            SetFirstCommand(m_CommandHistory.Peek());
            m_PlayerPredictionHistory = new CyclicArray<Container>(HistoryCount, () => new Container(elements.playerElements.Append(typeof(ClientStampComponent))));

            foreach (ServerSessionContainer session in m_SessionHistory)
            {
                session.RegisterAppend(typeof(LocalizedClientStampComponent));
                foreach (Container player in session.Require<PlayerArray>())
                    player.RegisterAppend(typeof(LocalizedClientStampComponent));
            }
        }

        public override void Start()
        {
            base.Start();
            m_Socket = new ComponentClientSocket(IpEndPoint, m_Injector.GetConnectWriter());
            m_Socket.Listener.PeerDisconnectedEvent += OnDisconnect;
            m_Socket.Receiver = this;
            RegisterMessages(m_Socket);

            Physics.autoSimulation = true;
        }

        private void OnDisconnect(NetPeer peer, DisconnectInfo disconnect)
        {
            if (disconnect.AdditionalData.TryGetString(out string reason))
                Debug.LogWarning($"Disconnected for reason: {reason}");
            Dispose();
        }

        private void UpdateInputs(Container player, int localPlayerId)
        {
            ClientCommandsContainer commands = m_CommandHistory.Peek();
            GetPlayerModifier(player, localPlayerId).ModifyCommands(this, commands, localPlayerId);
            _indexer = localPlayerId; // Prevent closure allocation
            _session = this;
            _container = commands;
            ForEachSessionInterface(@interface => @interface.ModifyLocalTrusted(_indexer, _session, _container));
        }

        public override Container GetLocalCommands() => m_CommandHistory.Peek();

        public override Container GetLocalPlayer() => m_PlayerPredictionHistory.Peek();

        protected override void Input(uint timeUs, uint durationUs)
        {
            Container verifiedLatestSession = GetLatestSession();
            if (!GetLocalPlayerId(verifiedLatestSession, out int localPlayerId))
                return;
            Container verifiedPlayer = GetModifyingPlayerFromId(localPlayerId, verifiedLatestSession);
            UpdateInputs(verifiedPlayer, localPlayerId);
            var context = new SessionContext(this, commands: m_CommandHistory.Peek(), playerId: localPlayerId, player: m_CommandHistory.Peek(),
                                             timeUs: timeUs, durationUs: durationUs);
            GetPlayerModifier(verifiedPlayer, localPlayerId).ModifyTrusted(context, verifiedPlayer);
        }

        // private static CyclicArray<Container> _predictionHistory;

        private static CyclicArray<Container> _predictionHistory;

        private static void MergeCommandInto(ElementBase element, ElementBase command) => ElementExtensions.NavigateZipped(element, command, (_element, _command) =>
        {
            if (_command.WithoutAttribute<ClientTrustedAttribute>()) return Navigation.Continue;
            _element.SetTo(_command);
            return Navigation.SkipDescendents;
        });
        
        protected override void Tick(uint tick, uint timeUs, uint durationUs)
        {
            Container latestSession = GetLatestSession();
            using (ClientPredictMarker.Auto())
            {
                if (GetLocalPlayerId(latestSession, out int localPlayerId))
                {
                    m_Injector.OnPreTick(latestSession);
                    Predict(tick, timeUs, localPlayerId); // Advances commands
                }
            }

            Profiler.BeginSample("Client Send");
            SendCommand();
            Profiler.EndSample();

            Profiler.BeginSample("Client Receive");
            Receive(timeUs);
            Profiler.EndSample();

            base.Tick(tick, timeUs, durationUs);
            m_Injector.OnPostTick(latestSession);

            Profiler.BeginSample("Client End");
            ClearSingleTicks(m_CommandHistory.Peek());
            Profiler.EndSample();
        }

        private void Receive(uint timeUs)
        {
            _timeUs = timeUs;
            m_Socket.PollEvents();
        }

        private static uint _timeUs;

        // private static readonly DurationAverage DurationAverage = new DurationAverage(60, "Navigate");

        void IReceiver.OnReceive(NetPeer fromPeer, NetDataReader reader, byte code)
        {
            switch (code)
            {
                case ServerSessionCode:
                {
                    Profiler.BeginSample("Client Receive Setup");
                    ServerSessionContainer previousServerSession = m_SessionHistory.Peek();

                    m_Injector.DeserializeReceived(m_EmptyServerSession, reader);
                    ServerSessionContainer receivedServerSession = m_EmptyServerSession;
                    uint serverTick = receivedServerSession.Require<ServerStampComponent>().tick;
                    bool everySecond = serverTick % 60 == 0;

                    UIntProperty previousServerTick = previousServerSession.Require<ServerStampComponent>().tick;
                    ServerSessionContainer serverSession;
                    var isMostRecent = true;
                    if (previousServerTick.WithValue)
                    {
                        var delta = checked((int) (serverTick - (long) previousServerTick));
                        if (delta > 0)
                        {
                            m_CommandHistory.Peek().Require<AcknowledgedServerTickProperty>().Value = serverTick;
                            for (var i = 0; i < delta - 1; i++) // We skipped tick(s). Reserve spaces to fill later
                            {
                                ServerSessionContainer reserved = m_SessionHistory.ClaimNext();
                                L.Warning($"[{GetType().Name}] Inserting out of order server update");
                                reserved.SetTo(previousServerSession);
                            }
                            serverSession = m_SessionHistory.ClaimNext();
                        }
                        else
                        {
                            // We received an old tick. Fill in history
                            serverSession = m_SessionHistory.Get(delta);
                            L.Warning($"[{GetType().Name}] Received out of order server update");
                            isMostRecent = false;
                        }
                    }
                    else serverSession = m_SessionHistory.ClaimNext();

                    UpdateCurrentSessionFromReceived(previousServerSession, serverSession, receivedServerSession);
                    
                    Profiler.EndSample();

                    m_Injector.OnClientReceive(serverSession);
                    RenderVerified(new SessionContext(this, serverSession));

                    if (!isMostRecent)
                    {
                        L.Warning("Is not most recent!");
                        break;
                    }

                    /* Most Recent */

                    {
                        // TODO:refactor make class

                        var serverStamp = serverSession.Require<ServerStampComponent>();
                        var previousServerStamp = serverSession.Require<LocalizedClientStampComponent>();
                        UIntProperty serverTimeUs = serverStamp.timeUs, localizedServerTimeUs = previousServerStamp.timeUs;

                        if (localizedServerTimeUs.WithValue)
                        {
                            uint previousTimeUs = previousServerSession.Require<ServerStampComponent>().timeUs;
                            if (serverTimeUs >= previousTimeUs) localizedServerTimeUs.Value += checked(serverTimeUs - previousTimeUs);
                            else Debug.LogError($"Updated server session time was less. Current: {serverStamp}; Previous: {previousServerStamp} μs");
                        }
                        else localizedServerTimeUs.Value = _timeUs;

                        long delta = localizedServerTimeUs.Value - (long) _timeUs;
                        if (Math.Abs(delta) > serverSession.Require<TickRateProperty>().TickIntervalUs * 3u)
                        {
                            ResetErrors++;
                            localizedServerTimeUs.Value = _timeUs;
                        }
                        AnalysisLogger.AddDataPoint(string.Empty, localizedServerTimeUs.Value, _timeUs, serverTimeUs.Value);
                    }

                    Profiler.BeginSample("Client Update Players");
                    var serverPlayers = serverSession.Require<PlayerArray>();
                    bool isLocalPlayerOnServer = GetLocalPlayerId(serverSession, out int localPlayerId);
                    for (var playerId = 0; playerId < serverPlayers.Length; playerId++)
                    {
                        Container serverPlayer = serverPlayers[playerId];
                        HealthProperty healthProperty = serverPlayer.Health();
                        UIntProperty localizedServerTimeUs = serverPlayer.Require<LocalizedClientStampComponent>().timeUs;
                        if (healthProperty.WithoutValue)
                            localizedServerTimeUs.Clear(); // Is something a client only has so we have to clear it
                        if (healthProperty.IsInactiveOrDead)
                            continue;
                        /* Valid player */

                        UIntProperty serverTimeUs = serverPlayer.Require<ServerStampComponent>().timeUs;

                        if (localizedServerTimeUs.WithValue)
                        {
                            uint previousTimeUs = previousServerSession.GetPlayer(playerId).Require<ServerStampComponent>().timeUs;
                            if (serverTimeUs >= previousTimeUs) localizedServerTimeUs.Value += checked(serverTimeUs - previousTimeUs);
                            else L.Warning($"Updated server player time was greater. Current: {serverTimeUs.Value} μs; Previous: {previousTimeUs} μs");
                        }
                        else localizedServerTimeUs.Value = _timeUs;

                        if (playerId != localPlayerId) GetPlayerModifier(serverPlayer, playerId).Synchronize(new SessionContext(player: serverPlayer));

                        long delta = localizedServerTimeUs.Value - (long) _timeUs;
                        if (Math.Abs(delta) > serverSession.Require<TickRateProperty>().TickIntervalUs * 3u)
                        {
                            ResetErrors++;
                            localizedServerTimeUs.Value = _timeUs;
                        }
                    }
                    Profiler.EndSample();

                    // Debug.Log($"{receivedServerSession.Require<ServerStampComponent>().time} {trackedTime.Value}");

                    Profiler.BeginSample("Client Check Prediction");
                    if (isLocalPlayerOnServer)
                    {
                        Container serverPlayer = serverSession.GetPlayer(localPlayerId);
                        CheckPrediction(serverPlayer, localPlayerId);
                    }
                    Profiler.EndSample();

                    Profiler.BeginSample("Write Demo");
                    try
                    {
                        if (WriteDemo) OnDemoCandidate(serverSession);
                        else if (m_DemoWriter.Length > 0) FlushDemo();
                    }
                    catch (Exception exception)
                    {
                        L.Exception(exception, "Error putting demo session");
                    }
                    Profiler.EndSample();

                    Profiler.BeginSample("Client Overrides");
                    ElementExtensions.NavigateZipped(serverSession.GetPlayer(localPlayerId), m_CommandHistory.Peek(), (_server, _command) =>
                    {
                        if (_server is PropertyBase serverProperty && serverProperty.IsOverride && _command is PropertyBase commandProperty)
                        {
                            commandProperty.SetTo(serverProperty);
                            Debug.Log($"Overriding with server: {serverProperty}");
                        }
                        return Navigation.Continue;
                    });
                    Profiler.EndSample();
                    break;
                }
            }
        }

        public static string GetDemoFile()
        {
            string parentFolder = Directory.GetParent(Application.dataPath).FullName;
            if (Application.platform == RuntimePlatform.OSXPlayer) parentFolder = Directory.GetParent(parentFolder).FullName;
            return Path.ChangeExtension(Path.Combine(parentFolder, "Demo"), "vfd");
        }

        private void OnDemoCandidate(Container session)
        {
            string demoFile = GetDemoFile();
            if (!File.Exists(demoFile))
            {
                m_DemoWriter.Reset();
                m_DemoWriter.Put(Application.version);
                using (new FileStream(demoFile, FileMode.Create))
                {
                }
            }
            if (session.Require<ServerStampComponent>().tick % (session.Require<TickRateProperty>().Value / DemoTickRate) != 0) return;
            session.Serialize(m_DemoWriter);
            if (m_DemoWriter.Length > DemoWriteSize)
                FlushDemo();
        }

        private void FlushDemo()
        {
            using (var stream = new FileStream(GetDemoFile(), FileMode.Append))
            {
                stream.Write(m_DemoWriter.Data, 0, m_DemoWriter.Length);
#if UNITY_EDITOR
                Debug.Log("Wrote demo section");
#endif
            }
            m_DemoWriter.Reset();
        }

        public static void ClearSingleTicks(ElementBase commands) => commands.Navigate(_element =>
        {
            if (!_element.TryAttribute(out SingleTickAttribute singleTickAttribute)) return Navigation.Continue;
            if (singleTickAttribute.Zero) _element.Zero();
            else _element.Clear();
            return Navigation.SkipDescendents;
        });

        private void SendCommand() => m_Socket.SendToServer(m_CommandHistory.Peek(), DeliveryMethod.ReliableUnordered);

        private static bool _predictionIsAccurate; // Prevents heap allocation in closure

        private static readonly Func<ElementBase, ElementBase, ElementBase, Navigation> VisitPredictedFunction = VisitPredicted;

        private void UpdateCurrentSessionFromReceived(Container previousServerSession, Container serverSession, Container receivedServerSession)
        {
            m_Injector.UpdateCurrentSessionFromReceived(previousServerSession, serverSession, receivedServerSession);
            // TODO:refactor need some sort of zip longest
            serverSession.Require<LocalizedClientStampComponent>().SetTo(previousServerSession.Require<LocalizedClientStampComponent>());
            var previousArray = previousServerSession.Require<PlayerArray>();
            var array = serverSession.Require<PlayerArray>();
            for (var playerIndex = 0; playerIndex < array.Length; playerIndex++)
                array[playerIndex].Require<LocalizedClientStampComponent>().SetTo(previousArray[playerIndex].Require<LocalizedClientStampComponent>());
        }

        private static bool GetLocalPlayerId(Container session, out int localPlayerId)
        {
            if (session.With(out LocalPlayerId localPlayerProperty) && localPlayerProperty.WithValue)
            {
                localPlayerId = localPlayerProperty;
                return true;
            }
            localPlayerId = default;
            return false;
        }

        public override Ray GetRayForPlayerId(int playerId) => m_PlayerPredictionHistory.Peek().GetRayForPlayer();

        protected override void RollbackHitboxes(in SessionContext context)
        {
            if (!DebugBehavior.Singleton.SendDebug) return;
            for (var i = 0; i < MaxPlayers; i++)
            {
                // int copiedPlayerId = i;
                // Container GetInHistory(int historyIndex) => m_SessionHistory.Get(-historyIndex).GetPlayer(copiedPlayerId);
                //
                // Container render = m_RenderSession.GetPlayer(i).Clone();
                //
                // float rollback = DebugBehavior.Singleton.RollbackOverride.OrElse(GetSettings().TickInterval) * 3;
                // RenderInterpolatedPlayer<LocalizedClientStampComponent>(Time.realtimeSinceStartup - rollback, render, m_SessionHistory.Size, GetInHistory);
                //
                // PlayerModifierDispatcherBehavior modifier = m_Modifier[i];
                // modifier.EvaluateHitboxes(i, render);

                Container recentPlayer = ((PlayerVisualsDispatcherBehavior) PlayerManager.UnsafeVisuals[i]).GetRecentPlayer();

                if (i == 0 && recentPlayer != null) SendDebug(recentPlayer);
            }
        }

        private void SendDebug(Container player)
        {
            DebugClientView debug = m_EmptyDebugClientView.Clone();
            debug.SetTo(player);
            m_Socket.SendToServer(debug, DeliveryMethod.ReliableOrdered);
        }

        public override void Dispose()
        {
            base.Dispose();
            m_Socket?.Dispose();
        }
    }
}