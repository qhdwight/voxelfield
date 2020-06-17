using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
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
                typeof(TickRateProperty), typeof(ModeIdProperty),
                typeof(PlayerContainerArrayElement), typeof(LocalPlayerProperty), typeof(EntityArrayElement),
                typeof(StampComponent), typeof(KillFeedElement)
            },
            playerElements = new List<Type>
            {
                typeof(HealthProperty), typeof(MoveComponent), typeof(InventoryComponent), typeof(CameraComponent), typeof(RespawnTimerProperty),
                typeof(TeamProperty), typeof(StatsComponent), typeof(HitMarkerComponent), typeof(DamageNotifierComponent), typeof(UsernameElement)
            },
            commandElements = new List<Type>
            {
                typeof(InputFlagProperty), typeof(WantedItemIndexProperty), typeof(MouseComponent)
            }
        };
    }

    public interface IPlayerContainerRenderer : IDisposable
    {
        void Setup(SessionBase session);

        // TODO:refactor is local player should use If construct
        void Render(int playerId, Container player, bool isLocalPlayer);

        Container GetRecentPlayer();
    }

    public class SessionInjectorBase
    {
        protected internal SessionBase Manager { get; set; }

        protected internal virtual bool IsPaused => false;

        protected internal virtual void OnSettingsTick(Container session) { }

        protected internal virtual void OnReceive(ServerSessionContainer serverSession) { }

        protected internal virtual void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession) { }

        protected internal virtual void OnHandleNewConnection(ConnectionRequest request) => request.Accept();
        
        protected internal virtual void Stop() { }
    }

    public abstract class SessionBase : IDisposable
    {
        internal const int MaxPlayers = 4;

        private readonly GameObject m_PlayerVisualsPrefab;
        protected readonly SessionElements m_SessionElements;
        protected readonly SessionInjectorBase m_Injector;
        protected readonly DefaultPlayerHud m_PlayerHud;
        protected readonly InterfaceBehaviorBase[] m_Interfaces;
        private long m_FixedUpdateTicks, m_RenderTicks;
        protected PlayerModifierDispatcherBehavior[] m_Modifier;
        protected IPlayerContainerRenderer[] m_Visuals;
        internal EntityManager EntityManager { get; } = new EntityManager();
        private uint m_Tick;
        private Stopwatch m_Stopwatch;
        public bool ShouldInterruptCommands { get; private set; }

        public SessionInjectorBase Injector => m_Injector;
        protected bool IsDisposed { get; private set; }
        public bool ShouldRender { get; set; } = true;
        public GameObject PlayerModifierPrefab { get; }

        protected SessionBase(SessionElements sessionElements, SessionInjectorBase injector)
        {
            m_SessionElements = sessionElements;
            m_Injector = injector;
            m_Injector.Manager = this;
            PlayerModifierPrefab = SessionGameObjectLinker.Singleton.GetPlayerModifierPrefab();
            m_PlayerVisualsPrefab = SessionGameObjectLinker.Singleton.GetPlayerVisualsPrefab();
            m_PlayerHud = UnityObject.FindObjectOfType<DefaultPlayerHud>();
            m_Interfaces = UnityObject.FindObjectsOfType<InterfaceBehaviorBase>();
        }

        public void SetApplicationPauseState(bool isPaused)
        {
            if (isPaused)
                m_Stopwatch.Stop();
            else
                m_Stopwatch.Start();
        }

        private T[] Instantiate<T>(GameObject prefab, int length, Action<int, T> setup)
        {
            return Enumerable.Range(0, length).Select(i =>
            {
                GameObject instance = UnityObject.Instantiate(prefab);
                instance.name = $"[{GetType().Name}] {prefab.name} ({i})";
                var component = instance.GetComponent<T>();
                setup(i, component);
                return component;
            }).ToArray();
        }

        public virtual void Start()
        {
            CheckDisposed();

            m_Stopwatch = Stopwatch.StartNew();

            m_Visuals = Instantiate<IPlayerContainerRenderer>(m_PlayerVisualsPrefab, MaxPlayers, (_, visuals) => visuals.Setup(this));
            m_Modifier = Instantiate<PlayerModifierDispatcherBehavior>(PlayerModifierPrefab, MaxPlayers, (i, modifier) => modifier.Setup(this, i));

            EntityManager.Setup(this);
            ForEachSessionInterface(sessionInterface => sessionInterface.SessionStateChange(true));
        }

        private void CheckDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException("Session was disposed");
        }

        private static uint GetUsFromTicks(long ticks) => checked((uint) Math.Round(ticks / (double) Stopwatch.Frequency * 1_000_000d));

        public void Update()
        {
            CheckDisposed();

            HandleCursorLockState();
            long clockTicks = m_Stopwatch.ElapsedTicks,
                 clockTickDelta = clockTicks - m_RenderTicks;
            m_RenderTicks = clockTicks;
            uint timeUs = GetUsFromTicks(clockTicks);
            Input(timeUs, GetUsFromTicks(clockTickDelta));
            if (ShouldRender) Render(timeUs);
        }

        private void HandleCursorLockState()
        {
            CursorLockMode desiredLockState;
            if (Application.isFocused)
            {
                desiredLockState = CursorLockMode.Locked;
                ShouldInterruptCommands = false;
                foreach (InterfaceBehaviorBase @interface in m_Interfaces)
                {
                    if (@interface.NeedsCursor)
                        desiredLockState = CursorLockMode.Confined;
                    if (@interface.InterruptsCommands)
                        ShouldInterruptCommands = true;
                }
            }
            else desiredLockState = CursorLockMode.None;

            bool desiredVisibility = desiredLockState != CursorLockMode.Locked;
            if (Cursor.lockState == desiredLockState && Cursor.visible == desiredVisibility) return;

            Cursor.lockState = desiredLockState;
            Cursor.visible = desiredVisibility;
        }

        protected virtual void Render(uint renderTimeUs) { }

        protected virtual void Tick(uint tick, uint timeUs, uint durationUs)
        {
            Container session = GetLatestSession();
            var tickRate = session.Require<TickRateProperty>();
            tickRate.SetFromIfWith(DebugBehavior.Singleton.TickRate);
            session.Require<ModeIdProperty>().SetFromIfWith(DebugBehavior.Singleton.ModeId);
            if (tickRate.WithValue) Time.fixedDeltaTime = 1.0f / tickRate;
        }

        protected virtual void Input(uint timeUs, uint deltaUs) { }

        public void FixedUpdate()
        {
            CheckDisposed();

            long clockTicks = m_Stopwatch.ElapsedTicks,
                 clockTickDelta = clockTicks - m_FixedUpdateTicks;
            m_FixedUpdateTicks = clockTicks;
            Tick(m_Tick++, GetUsFromTicks(clockTicks), GetUsFromTicks(clockTickDelta));
        }

        protected static void RenderInterpolated
            (uint renderTimeUs, Container renderContainer, int maxRollback, Func<int, StampComponent> getTimeInHistory, Func<int, Container> getInHistory)
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
                    renderContainer.CopyFrom(getInHistory(0));
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
            renderContainer.CopyFrom(getInHistory(1));
        }

        protected static void RenderInterpolatedPlayer<TStampComponent>
            (uint renderTimeUs, Container renderContainer, int maxRollback, Func<int, Container> getInHistory)
            where TStampComponent : StampComponent
        {
            RenderInterpolated(renderTimeUs, renderContainer, maxRollback,
                               historyIndex => getInHistory(historyIndex).Require<TStampComponent>(), getInHistory);
        }

        public abstract Ray GetRayForPlayerId(int playerId);

        public virtual void RollbackHitboxesFor(int playerId)
        {
            // Usually transform sync happens after FixedUpdate() is called. However, our raycast is in fixed update.
            // So, we need to preemptively force the colliders in the hitbox to update.
            // Otherwise, there is always a one tick lag.
            Physics.SyncTransforms();
        }

        public abstract ModeBase GetMode(Container session = null);

        public abstract Container GetLatestSession();

        public static Ray GetRayForPlayer(Container player)
        {
            var camera = player.Require<CameraComponent>();
            float yaw = camera.yaw * Mathf.Deg2Rad, pitch = camera.pitch * Mathf.Deg2Rad;
            // Convert from spherical coordinates to cartesian vector
            var direction = new Vector3(Mathf.Cos(pitch) * Mathf.Sin(yaw), -Mathf.Sin(pitch), Mathf.Cos(pitch) * Mathf.Cos(yaw));
            direction.Normalize();
            var move = player.Require<MoveComponent>();
            // TODO:refactor magic numbers
            Vector3 position = move.position + new Vector3 {y = Mathf.Lerp(1.26f, 1.8f, 1.0f - move.normalizedCrouch)};

            var ray = new Ray(position, direction);
            Debug.DrawLine(position, position + direction * 10.0f, Color.blue, 5.0f);
            return ray;
        }

        public virtual Container GetPlayerFromId(int playerId) { throw new NotImplementedException(); }

        protected void ForEachSessionInterface(Action<SessionInterfaceBehavior> action)
        {
            foreach (InterfaceBehaviorBase @interface in m_Interfaces)
                if (@interface is SessionInterfaceBehavior sessionInterface)
                    action(sessionInterface);
        }

        public virtual void Dispose()
        {
            IsDisposed = true;
            foreach (PlayerModifierDispatcherBehavior modifier in m_Modifier) modifier.Dispose();
            foreach (IPlayerContainerRenderer visual in m_Visuals) visual.Dispose();
            ForEachSessionInterface(sessionInterface => sessionInterface.SessionStateChange(false));
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            m_Injector.Stop();
            m_Stopwatch.Stop();
        }

        public virtual bool IsPaused => m_Injector.IsPaused;
    }
}