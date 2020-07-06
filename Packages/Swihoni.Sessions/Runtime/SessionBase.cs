using System;
using System.Collections.Generic;
using System.Diagnostics;
using LiteNetLib;
using Swihoni.Components;
using Swihoni.Sessions.Components;
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
                typeof(TickRateProperty), typeof(ModeIdProperty),
                typeof(PlayerContainerArrayElement), typeof(LocalPlayerId), typeof(EntityArrayElement),
                typeof(StampComponent), typeof(KillFeedElement)
            },
            playerElements = new List<Type>
            {
                typeof(HealthProperty), typeof(IdProperty), typeof(MoveComponent), typeof(FrozenProperty), typeof(InventoryComponent),
                typeof(CameraComponent), typeof(RespawnTimerProperty),
                typeof(TeamProperty), typeof(StatsComponent), typeof(HitMarkerComponent), typeof(DamageNotifierComponent), typeof(UsernameProperty),
                typeof(StringCommandProperty)
            },
            commandElements = new List<Type>
            {
                typeof(InputFlagProperty), typeof(WantedItemIndexProperty), typeof(MouseComponent)
            }
        };
    }

    public class SessionInjectorBase
    {
        protected internal SessionBase Manager { get; set; }

        protected internal virtual void OnSettingsTick(Container session) { }

        protected internal virtual void OnReceive(ServerSessionContainer serverSession) { }

        protected internal virtual void OnSendInitialData(NetPeer peer, Container serverSession, Container sendSession) { }

        protected internal virtual void OnHandleNewConnection(ConnectionRequest request) => request.Accept();

        protected internal virtual void Stop() { }

        protected internal virtual Vector3 GetSpawnPosition() => new Vector3 {y = 10.0f};

        public virtual bool IsPaused(Container session) => false;
        
        public virtual void OnThrowablePopped(ThrowableModifierBehavior throwableBehavior) {  }
    }

    public abstract class SessionBase : IDisposable
    {
        public const int MaxPlayers = 4;

        protected readonly SessionInjectorBase m_Injector;
        protected readonly InterfaceBehaviorBase[] m_Interfaces;
        private long m_FixedUpdateTicks, m_RenderTicks;
        private uint m_Tick;
        private Stopwatch m_Stopwatch;
        private ModeBase m_Mode;
        public bool ShouldInterruptCommands { get; private set; }

        public SessionInjectorBase Injector => m_Injector;
        public PlayerManager PlayerManager { get; private set; }
        public EntityManager EntityManager { get; private set; }
        protected bool IsDisposed { get; private set; }
        public bool ShouldRender { get; set; } = true;

        protected SessionBase(SessionElements sessionElements, SessionInjectorBase injector)
        {
            m_Injector = injector;
            m_Injector.Manager = this;
            m_Interfaces = UnityObject.FindObjectsOfType<InterfaceBehaviorBase>();
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
        }

        protected PlayerModifierDispatcherBehavior GetPlayerModifier(Container player, int index)
        {
            var modifier = (PlayerModifierDispatcherBehavior) PlayerManager.GetModifierAtIndex(player, index, out bool isNewlyObtained);
            if (isNewlyObtained) modifier.Setup(this, index);
            return modifier;
        }

        protected PlayerVisualsDispatcherBehavior GetPlayerVisuals(Container player, int index) => (PlayerVisualsDispatcherBehavior) PlayerManager.GetVisualAtIndex(player, index);

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
            if (!IsPaused) Input(timeUs, GetUsFromTicks(clockTickDelta));
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

        public abstract Container GetLatestSession();

        public static Ray GetRayForPlayer(Container player)
        {
            var camera = player.Require<CameraComponent>();
            // Convert from spherical coordinates to cartesian vector
            Vector3 direction = camera.GetForward();
            var move = player.Require<MoveComponent>();
            // TODO:refactor magic numbers
            Vector3 position = move.position + new Vector3 {y = Mathf.Lerp(1.26f, 1.8f, 1.0f - move.normalizedCrouch)};

            var ray = new Ray(position, direction);
            Debug.DrawLine(position, position + direction * 10.0f, Color.blue, 5.0f);
            return ray;
        }

        /// <param name="session">If null, return settings from most recent history. Else get from specified session.</param>
        public virtual ModeBase GetMode(Container session = null)
        {
            ModeBase mode = ModeManager.GetMode((session ?? GetLatestSession()).Require<ModeIdProperty>());
            if (!m_Mode || m_Mode != mode)
            {
                if (m_Mode) m_Mode.End();
                mode.Begin(this, session);
            }
            return m_Mode = mode;
        }

        public abstract Container GetPlayerFromId(int playerId, Container session = null);

        protected void ForEachSessionInterface(Action<SessionInterfaceBehavior> action)
        {
            foreach (InterfaceBehaviorBase @interface in m_Interfaces)
                if (@interface is SessionInterfaceBehavior sessionInterface)
                    action(sessionInterface);
        }

        public virtual bool IsPaused => m_Injector.IsPaused(GetLatestSession());

        public virtual void Dispose()
        {
            IsDisposed = true;
            PlayerManager.pool.Return(PlayerManager);
            ForEachSessionInterface(sessionInterface => sessionInterface.SessionStateChange(false));
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
            EntityManager.pool.Return(EntityManager);
            if (m_Mode) m_Mode.End();
            m_Injector.Stop();
            m_Stopwatch.Stop();
        }

        public abstract void StringCommand(int playerId, string stringCommand);
    }
}