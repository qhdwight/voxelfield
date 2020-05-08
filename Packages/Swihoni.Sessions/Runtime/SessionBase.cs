using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Interface;
using UnityEngine;
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
                typeof(TickRateProperty), typeof(ModeIdProperty), typeof(PauseComponent),
                typeof(PlayerContainerArrayProperty), typeof(LocalPlayerProperty), typeof(EntityArrayProperty),
                typeof(StampComponent), typeof(KillFeedProperty)
            },
            playerElements = new List<Type>
            {
                typeof(HealthProperty), typeof(MoveComponent), typeof(InventoryComponent), typeof(CameraComponent), typeof(RespawnTimerProperty),
                typeof(TeamProperty), typeof(StatsComponent), typeof(HitMarkerComponent), typeof(DamageNotifierComponent)
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

        // TODO: refactor is local player should use If construct
        void Render(int playerId, Container player, bool isLocalPlayer);

        Container GetRecentPlayer();
    }

    public abstract class SessionBase : IDisposable
    {
        internal const int MaxPlayers = 3;

        private readonly GameObject m_PlayerVisualsPrefab;

        protected readonly DefaultPlayerHud m_PlayerHud;
        protected readonly InterfaceBehaviorBase[] m_Interfaces;
        private float m_FixedUpdateTime, m_RenderTime;
        protected PlayerModifierDispatcherBehavior[] m_Modifier;
        protected IPlayerContainerRenderer[] m_Visuals;
        internal EntityManager EntityManager { get; } = new EntityManager();
        private uint m_Tick;
        public bool ShouldInterruptCommands { get; private set; }

        protected bool IsDisposed { get; private set; }
        public bool ShouldRender { get; set; } = true;
        public GameObject PlayerModifierPrefab { get; }

        protected SessionBase()
        {
            PlayerModifierPrefab = SessionGameObjectLinker.Singleton.GetPlayerModifierPrefab();
            m_PlayerVisualsPrefab = SessionGameObjectLinker.Singleton.GetPlayerVisualsPrefab();
            m_PlayerHud = UnityObject.FindObjectOfType<DefaultPlayerHud>();
            m_Interfaces = UnityObject.FindObjectsOfType<InterfaceBehaviorBase>();
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

            m_Visuals = Instantiate<IPlayerContainerRenderer>(m_PlayerVisualsPrefab, MaxPlayers, (_, visuals) => visuals.Setup(this));
            m_Modifier = Instantiate<PlayerModifierDispatcherBehavior>(PlayerModifierPrefab, MaxPlayers, (i, modifier) => modifier.Setup(this, i));

            EntityManager.Setup(this);
        }

        private void CheckDisposed()
        {
            if (IsDisposed) throw new ObjectDisposedException("Session was disposed");
        }

        public void Update(float time)
        {
            CheckDisposed();

            HandleCursorLockState();
            float delta = time - m_RenderTime;
            Input(time, delta);
            if (ShouldRender) Render(time);
            m_RenderTime = time;
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
            else
                desiredLockState = CursorLockMode.None;

            bool desiredVisibility = desiredLockState != CursorLockMode.Locked;
            if (Cursor.lockState == desiredLockState && Cursor.visible == desiredVisibility) return;

            Cursor.lockState = desiredLockState;
            Cursor.visible = desiredVisibility;
        }

        protected virtual void Render(float renderTime) { }

        protected virtual void Tick(uint tick, float time, float duration)
        {
            GetLatestSession().Require<TickRateProperty>().IfPresent(tickRate => Time.fixedDeltaTime = 1.0f / tickRate);
        }

        protected virtual void Input(float time, float delta) { }

        public void FixedUpdate(float time)
        {
            if (IsDisposed) throw new ObjectDisposedException("Session disposed");

            float duration = time - m_FixedUpdateTime;
            m_FixedUpdateTime = time;
            Tick(m_Tick++, time, duration);
        }

        // protected void InterpolateHistoryInto<TComponent>(TComponent componentToInterpolate, CyclicArray<TComponent> componentHistory,
        //                                                   Func<TComponent, float> getDuration, float rollback, float timeSinceLastUpdate)
        //     where TComponent : ComponentBase
        // {
        //     InterpolateHistoryInto(componentToInterpolate, i => componentHistory.Get(i), componentHistory.Size, getDuration, rollback, timeSinceLastUpdate);
        // }
        //
        // protected static void InterpolateHistoryInto<TComponent>(TComponent componentToInterpolate, Func<int, TComponent> getInHistory, int maxRollback,
        //                                                          Func<int, float> getDuration, float rollback, float timeSinceLastUpdate)
        //     where TComponent : ComponentBase
        // {
        //     int fromIndex = 0, toIndex = 0;
        //     var durationCount = 0.0f;
        //     for (var historyIndex = 0; historyIndex < maxRollback; historyIndex++)
        //     {
        //         fromIndex = -historyIndex - 1;
        //         toIndex = -historyIndex;
        //         durationCount += getDuration(-historyIndex);
        //         if (durationCount >= rollback - timeSinceLastUpdate) break;
        //         if (historyIndex != maxRollback - 1) continue;
        //         // We do not have enough history. Copy the most recent instead
        //         componentToInterpolate.MergeSet(getInHistory(0));
        //         return;
        //     }
        //     float interpolation;
        //     if (getDuration(toIndex) > 0.0f)
        //     {
        //         float elapsed = durationCount - rollback + timeSinceLastUpdate;
        //         interpolation = elapsed / getDuration(toIndex);
        //     }
        //     else
        //         interpolation = 0.0f;
        //     Interpolator.InterpolateInto(getInHistory(fromIndex), getInHistory(toIndex), componentToInterpolate, interpolation);
        // }

        protected static void RenderInterpolated
            (float renderTime, Container renderContainer, int maxRollback, Func<int, StampComponent> getTimeInHistory, Func<int, Container> getInHistory)
        {
            // Interpolate all remote players
            for (var historyIndex = 0; historyIndex < maxRollback; historyIndex++)
            {
                Container fromComponent = getInHistory(historyIndex + 1),
                          toComponent = getInHistory(historyIndex);
                FloatProperty toTime = getTimeInHistory(historyIndex).time,
                              fromTime = getTimeInHistory(historyIndex + 1).time;
                if (!toTime.HasValue || !fromTime.HasValue || (historyIndex == 0 && toTime < renderTime))
                {
                    renderContainer.FastCopyFrom(getInHistory(0));
                    // Debug.LogWarning("Not enough history");
                    return;
                }
                if (renderTime > fromTime && renderTime < toTime)
                {
                    float interpolation = (renderTime - fromTime) / (toTime - fromTime);
                    Interpolator.InterpolateInto(fromComponent, toComponent, renderContainer, interpolation);
                    return;
                }
            }
            // Take last if we do not have enough history
            // Debug.LogWarning("Not enough recent");
            renderContainer.FastCopyFrom(getInHistory(-1));
        }

        protected static void RenderInterpolatedPlayer<TStampComponent>
            (float renderTime, Container renderContainer, int maxRollback, Func<int, Container> getInHistory)
            where TStampComponent : StampComponent
        {
            RenderInterpolated(renderTime, renderContainer, maxRollback,
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

        public virtual void Dispose()
        {
            IsDisposed = true;
            foreach (PlayerModifierDispatcherBehavior modifier in m_Modifier) modifier.Dispose();
            foreach (IPlayerContainerRenderer visual in m_Visuals) visual.Dispose();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }

        public bool IsPaused
        {
            get => GetLatestSession().Require<PauseComponent>().Value;
            set => GetLatestSession().Require<PauseComponent>().Value = value;
        }
    }
}