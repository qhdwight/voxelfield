using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using LiteNetLib;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Sessions.Player.Visualization;
using Swihoni.Util.Interface;
using UnityEngine;
using Debug = UnityEngine.Debug;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions
{
    public class SessionElements
    {
        public List<Type> elements, playerElements, commandElements;

        public static SessionElements NewStandardSessionElements() => new SessionElements
        {
            elements = new List<Type>
            {
                typeof(TickRateProperty), typeof(ModeIdProperty), typeof(AllowCheatsProperty),
                typeof(PlayerContainerArrayElement), typeof(LocalPlayerId), typeof(EntityArrayElement),
                typeof(StampComponent), typeof(KillFeedElement)
            },
            playerElements = new List<Type>
            {
                typeof(HealthProperty), typeof(IdProperty), typeof(MoveComponent), typeof(FrozenProperty), typeof(InventoryComponent),
                typeof(CameraComponent), typeof(RespawnTimerProperty),
                typeof(TeamProperty), typeof(StatsComponent), typeof(HitMarkerComponent), typeof(DamageNotifierComponent), typeof(UsernameProperty),
                typeof(StringCommandProperty), typeof(ChatEntryProperty)
            },
            commandElements = new List<Type>
            {
                typeof(InputFlagProperty), typeof(WantedItemIndexProperty), typeof(MouseComponent), typeof(WantedTeamProperty)
            }
        };
    }

    public class SessionInjectorBase
    {
        protected internal SessionBase Session { get; set; }

        protected internal virtual void OnSettingsTick(Container session) { }

        protected internal virtual void OnReceive(ServerSessionContainer serverSession) { }

        protected internal virtual void OnRenderMode(Container session) { }

        protected internal virtual void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession) { }

        protected internal virtual void OnServerNewConnection(ConnectionRequest socketRequest) => socketRequest.AcceptIfKey(Application.version);

        protected internal virtual void Dispose() { }

        public virtual bool IsLoading(Container session) => false;

        public virtual void OnThrowablePopped(ThrowableModifierBehavior throwableBehavior) { }

        public virtual void OnReceiveCode(NetPeer fromPeer, NetDataReader reader, byte code) { }

        public virtual void OnStart() { }

        public virtual NetDataWriter GetConnectWriter()
        {
            var writer = new NetDataWriter();
            writer.Put(Application.version);
            return writer;
        }

        public virtual void OnServerLoseConnection(NetPeer peer, Container player) { }

        public virtual void OnKillPlayer(in DamageContext context) { }

        public virtual void OnPlayerRegisterAppend(Container player) { }

        public virtual string GetUsername(in ModifyContext context) => $"Player #{context.playerId}";

        public virtual void OnSetupHost(in ModifyContext context) { }

        public virtual void OnStop() { }

        public virtual bool ShouldSetupPlayer(Container serverPlayer) => serverPlayer.Require<HealthProperty>().WithoutValue;

        public virtual void OnServerModify(in ModifyContext context, MoveComponent component) { }
    }

    public abstract class SessionBase : IDisposable
    {
        public const int MaxPlayers = 6;

        private static List<SessionBase> _sessions, _sessionList;
        private static InterfaceBehaviorBase[] _interfaces;

        public static int SessionCount => _sessions.Count;

        public static IEnumerable<SessionBase> SessionEnumerable
        {
            get
            {
                _sessionList.Clear();
                _sessionList.AddRange(_sessions);
                return _sessionList;
            }
        }

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeInterfaces() => _interfaces = UnityObject.FindObjectsOfType<InterfaceBehaviorBase>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            _sessions = new List<SessionBase>(1);
            _sessionList = new List<SessionBase>(1);
        }

        protected readonly SessionInjectorBase m_Injector;
        private long m_FixedUpdateTicks, m_RenderTicks;
        private uint m_Tick;
        private Stopwatch m_Stopwatch;
        private ModeBase m_Mode;
        public static InterfaceBehaviorBase InterruptingInterface { get; private set; }

        public SessionInjectorBase Injector => m_Injector;
        public PlayerManager PlayerManager { get; private set; }
        public EntityManager EntityManager { get; private set; }
        protected bool IsDisposed { get; private set; }
        public bool ShouldRender { get; set; } = true;

        protected SessionBase(SessionElements sessionElements, SessionInjectorBase injector)
        {
            m_Injector = injector;
            m_Injector.Session = this;
        }

        public static Camera ActiveCamera => Camera.allCameras.First(camera => !camera.targetTexture);

        public static void RegisterSessionCommand(params string[] commands)
        {
            foreach (string command in commands)
                ConsoleCommandExecutor.SetCommand(command, IssueCommand);
        }

        public static void IssueCommand(string[] args)
        {
            foreach (SessionBase session in SessionEnumerable)
                session.StringCommand(session.GetLatestSession().Require<LocalPlayerId>(), string.Join(" ", args));
        }

        public void SetApplicationPauseState(bool isPaused)
        {
            if (isPaused) m_Stopwatch.Stop();
            else m_Stopwatch.Start();
        }

        public virtual void Start()
        {
            CheckDisposed();

            m_Stopwatch = Stopwatch.StartNew();

            PlayerManager = PlayerManager.pool.Obtain();
            PlayerManager.Setup(this);

            EntityManager = EntityManager.pool.Obtain();
            EntityManager.Setup(this);

            ForEachSessionInterface(sessionInterface => sessionInterface.SessionStateChange(true));

            _sessions.Add(this);

            m_Injector.OnStart();
        }

        public virtual void Stop()
        {
            if (!IsDisposed)
            {
                try
                {
                    m_Injector.OnStop();
                }
                finally
                {
                    Dispose();
                }
            }
        }

        public virtual int GetPeerPlayerId(NetPeer peer) => peer.Id;

        public PlayerModifierDispatcherBehavior GetPlayerModifier(Container player, int index)
        {
            var modifier = (PlayerModifierDispatcherBehavior) PlayerManager.GetModifierAtIndex(player, index, out bool isNewlyObtained);
            if (isNewlyObtained) modifier.Setup(this, index);
            return modifier;
        }

        protected PlayerVisualsDispatcherBehavior GetPlayerVisuals(Container player, int index) => (PlayerVisualsDispatcherBehavior) PlayerManager.GetVisualAtIndex(player, index);

        private void CheckDisposed()
        {
            if (IsDisposed)
            {
                Stop();
                throw new ObjectDisposedException("Session was disposed");
            }
        }

        private static uint GetUsFromTicks(long ticks) => checked((uint) Math.Round(ticks / (double) Stopwatch.Frequency * 1_000_000d));

        public void Update()
        {
            CheckDisposed();

            long clockTicks = m_Stopwatch.ElapsedTicks,
                 clockTickDelta = clockTicks - m_RenderTicks;
            m_RenderTicks = clockTicks;
            uint timeUs = GetUsFromTicks(clockTicks);
            if (!IsLoading) Input(timeUs, GetUsFromTicks(clockTickDelta));
            if (ShouldRender) Render(timeUs);
        }

        public static void HandleCursorLockState()
        {
            CursorLockMode desiredLockState;
            if (Application.isFocused)
            {
                desiredLockState = CursorLockMode.Locked;
                InterruptingInterface = null;
                foreach (InterfaceBehaviorBase @interface in _interfaces)
                {
                    if (@interface.NeedsCursor)
                        desiredLockState = CursorLockMode.None;
                    if (@interface.InterruptsCommands)
                        InterruptingInterface = @interface;
                }
            }
            else desiredLockState = CursorLockMode.None;

            bool desiredVisibility = desiredLockState != CursorLockMode.Locked;
            if (Cursor.lockState == desiredLockState && Cursor.visible == desiredVisibility) return;

            Cursor.lockState = desiredLockState;
            Cursor.visible = desiredVisibility;
        }

        public bool IsValidLocalPlayer(Container sessionContainer, out Container localPlayer, bool needsToBeAlive = true)
        {
            var localPlayerId = sessionContainer.Require<LocalPlayerId>();
            if (localPlayerId.WithoutValue)
            {
                localPlayer = default;
                return false;
            }
            localPlayer = GetModifyingPayerFromId(localPlayerId);
            return !needsToBeAlive || localPlayer.Require<HealthProperty>().IsActiveAndAlive;
        }

        protected virtual void Render(uint renderTimeUs) { }

        protected virtual void Tick(uint tick, uint timeUs, uint durationUs)
        {
            Container session = GetLatestSession();
            var tickRate = session.Require<TickRateProperty>();
            if (tickRate.WithValue) Time.fixedDeltaTime = 1.0f / tickRate;
        }

        protected virtual void Input(uint timeUs, uint durationUs) { }

        public void FixedUpdate()
        {
            CheckDisposed();

            long clockTicks = m_Stopwatch.ElapsedTicks,
                 clockTickDelta = clockTicks - m_FixedUpdateTicks;
            m_FixedUpdateTicks = clockTicks;
            Tick(m_Tick++, GetUsFromTicks(clockTicks), GetUsFromTicks(clockTickDelta));
        }

        protected static void RenderInterpolated(uint renderTimeUs, Container renderContainer, int maxRollback,
                                                 Func<int, StampComponent> getTimeInHistory, Func<int, Container> getInHistory)
        {
            // Interpolate all remote players
            for (var historyIndex = 0; historyIndex < maxRollback; historyIndex++)
            {
                Container fromComponent = getInHistory(historyIndex + 1),
                          toComponent = getInHistory(historyIndex);
                UIntProperty toTimeUs = getTimeInHistory(historyIndex).timeUs,
                             fromTimeUs = getTimeInHistory(historyIndex + 1).timeUs;
                var tooRecent = false;
                if (!toTimeUs.WithValue || !fromTimeUs.WithValue || (tooRecent = historyIndex == 0 && toTimeUs < renderTimeUs))
                {
                    renderContainer.SetTo(getInHistory(0));
                    // if (tooRecent) Debug.LogWarning("Not enough recent");
                    return;
                }
                if (renderTimeUs >= fromTimeUs && renderTimeUs <= toTimeUs)
                {
                    float interpolation = (renderTimeUs - fromTimeUs) / (float) (toTimeUs - fromTimeUs);
                    Interpolator.InterpolateInto(fromComponent, toComponent, renderContainer, interpolation);
                    return;
                }
                if (historyIndex == maxRollback - 1)
                {
                    Debug.Log($"{renderTimeUs}, {fromTimeUs}, {toTimeUs}");
                }
            }
            // Take last if we do not have enough history
            // Debug.LogWarning("Not enough history");
            renderContainer.SetTo(getInHistory(1));
        }

        protected static Func<int, Container> _getInHistory;

        protected interface IHistory
        {
            Container Get(int index);
        }

        protected static void RenderInterpolatedPlayer<TStampComponent>(uint renderTimeUs, Container renderContainer, int maxRollback,
                                                                        Func<int, Container> getInHistory)
            where TStampComponent : StampComponent
        {
            _getInHistory = getInHistory; // Prevent allocation in closure
            RenderInterpolated(renderTimeUs, renderContainer, maxRollback,
                               historyIndex => _getInHistory(historyIndex).Require<TStampComponent>(), _getInHistory);
        }

        public abstract Ray GetRayForPlayerId(int playerId);

        public virtual void RollbackHitboxesFor(int playerId)
        {
            // Usually transform sync happens after FixedUpdate() is called. However, our raycast is in fixed update.
            // So, we need to preemptively force the colliders in the hitbox to update.
            // Otherwise, there is always a one tick lag.
            Physics.SyncTransforms();
        }

        public abstract Container GetLatestSession();

        public virtual Container GetLocalCommands() => throw new NotImplementedException();

        public static Ray GetRayForPlayer(Container player)
        {
            var camera = player.Require<CameraComponent>();
            // Convert from spherical coordinates to cartesian vector
            Vector3 direction = camera.GetForward();
            var move = player.Require<MoveComponent>();
            // TODO:refactor magic numbers
            Vector3 position = GetPlayerEyePosition(move);

            var ray = new Ray(position, direction);
            return ray;
        }

        public static Vector3 GetPlayerEyePosition(MoveComponent move) => move.position + new Vector3 {y = Mathf.Lerp(1.26f, 1.8f, 1.0f - move.normalizedCrouch)};

        /// <param name="session">If null, return settings from most recent history. Else get from specified session.</param>
        public virtual ModeBase GetModifyingMode(Container session = null)
        {
            session = session ?? GetLatestSession();
            ModeBase mode = ModeManager.GetMode(session);
            if (!m_Mode || m_Mode != mode)
            {
                var modify = new ModifyContext(this, session);
                if (m_Mode) m_Mode.EndModify(modify);
                mode.BeginModify(modify);
            }
            return m_Mode = mode;
        }

        public abstract Container GetModifyingPayerFromId(int playerId, Container session = null);

        protected static void ForEachSessionInterface(Action<SessionInterfaceBehavior> action)
        {
            foreach (InterfaceBehaviorBase @interface in _interfaces)
                if (@interface is SessionInterfaceBehavior sessionInterface)
                    action(sessionInterface);
        }

        public virtual bool IsLoading => m_Injector.IsLoading(GetLatestSession());

        public virtual void Dispose()
        {
            try
            {
                IsDisposed = true;
                PlayerManager.pool.Return(PlayerManager);
                ForEachSessionInterface(sessionInterface => sessionInterface.SessionStateChange(false));
                EntityManager.pool.Return(EntityManager);
                m_Injector.Dispose();
                m_Stopwatch.Stop();
            }
            finally
            {
                _sessions.Remove(this);
            }
        }

        public abstract void StringCommand(int playerId, string stringCommand);

        public static StringBuilder BuildUsername(Container sessionContainer, StringBuilder builder, Container player)
            => ModeManager.GetMode(sessionContainer).BuildUsername(builder, player);
    }

    public static class SessionExtensions
    {
        public static Container GetPlayer(this Container session, int index) => session.Require<PlayerContainerArrayElement>()[index];

        public static string ExecuteProcess(string command)
        {
            int firstSpace = command.IndexOf(" ", StringComparison.Ordinal);
            var processInfo = new ProcessStartInfo {CreateNoWindow = true, UseShellExecute = false, RedirectStandardOutput = true};
            if (firstSpace == -1) processInfo.FileName = command;
            else
            {
                processInfo.FileName = command.Substring(0, firstSpace);
                processInfo.Arguments = command.Substring(firstSpace + 1);
            }
            Process process = Process.Start(processInfo);
            if (process != null)
            {
                process.WaitForExit();
                string result = process.StandardOutput.ReadToEnd();
                process.Close();
                return result;
            }
            return string.Empty;
        }
    }
}