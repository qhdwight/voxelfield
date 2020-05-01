using System;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions
{
    public interface IPlayerContainerRenderer : IDisposable
    {
        void Setup(SessionBase session);

        // TODO: refactor is local player should use If construct
        void Render(int playerId, Container player, bool isLocalPlayer);
    }

    [Serializable]
    public class FeedComponent : ComponentBase
    {
        public StringProperty feed;
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
        private uint m_Tick;
        public bool ShouldInterruptCommands { get; private set; }

        protected bool IsDisposed { get; private set; }
        public bool ShouldRender { get; set; } = true;
        public GameObject PlayerModifierPrefab { get; }
        public DefaultPlayerHud PlayerHud => m_PlayerHud;

        protected SessionBase(ISessionGameObjectLinker linker)
        {
            PlayerModifierPrefab = linker.GetPlayerModifierPrefab();
            m_PlayerVisualsPrefab = linker.GetPlayerVisualsPrefab();
            m_PlayerHud = UnityObject.FindObjectOfType<DefaultPlayerHud>();
            m_Interfaces = UnityObject.FindObjectsOfType<InterfaceBehaviorBase>();
        }

        private T[] Instantiate<T>(GameObject prefab, int length, Action<T> setup)
        {
            return Enumerable.Range(0, length).Select(i =>
            {
                GameObject instance = UnityObject.Instantiate(prefab);
                instance.name = $"[{GetType().Name}] {prefab.name} ({i})";
                var component = instance.GetComponent<T>();
                setup(component);
                return component;
            }).ToArray();
        }

        public virtual void Start()
        {
            CheckDisposed();

            m_Visuals = Instantiate<IPlayerContainerRenderer>(m_PlayerVisualsPrefab, MaxPlayers, visuals => visuals.Setup(this));
            m_Modifier = Instantiate<PlayerModifierDispatcherBehavior>(PlayerModifierPrefab, MaxPlayers, modifier => modifier.Setup(this));
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
            var desiredLockState = CursorLockMode.Locked;
            ShouldInterruptCommands = false;
            foreach (InterfaceBehaviorBase @interface in m_Interfaces)
            {
                if (@interface.NeedsCursor)
                    desiredLockState = CursorLockMode.Confined;
                if (@interface.InterruptsCommands)
                    ShouldInterruptCommands = true;
            }
            bool desiredVisibility = desiredLockState != CursorLockMode.Locked;
            if (Cursor.lockState == desiredLockState && Cursor.visible == desiredVisibility) return;

            Cursor.lockState = desiredLockState;
            Cursor.visible = desiredVisibility;
        }

        protected virtual void Render(float renderTime) { }

        protected virtual void Tick(uint tick, float time, float duration) { }

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

        protected static void RenderInterpolatedPlayer<TStampComponent>(float renderTime, Container renderPlayerContainer, int maxRollback, Func<int, Container> getInHistory)
            where TStampComponent : StampComponent
        {
            // Interpolate all remote players
            for (var historyIndex = 0; historyIndex < maxRollback; historyIndex++)
            {
                Container fromComponent = getInHistory(historyIndex + 1),
                          toComponent = getInHistory(historyIndex);
                FloatProperty toTime = toComponent.Require<TStampComponent>().time,
                              fromTime = fromComponent.Require<TStampComponent>().time;
                if (!fromTime.HasValue || !toTime.HasValue) break;
                if (renderTime > fromTime && renderTime < toTime)
                {
                    float interpolation = (renderTime - fromTime) / (toTime - fromTime);
                    Interpolator.InterpolateInto(fromComponent, toComponent, renderPlayerContainer, interpolation);
                    return;
                }
            }
            // Take last if we do not have enough history
            renderPlayerContainer.FastCopyFrom(getInHistory(0));
        }

        public abstract Ray GetRayForPlayerId(int playerId);

        public virtual void AboutToRaycast(int playerId)
        {
            // Usually transform sync happens after FixedUpdate() is called. However, our raycast is in fixed update.
            // So, we need to preemptively force the colliders in the hitbox to update.
            // Otherwise, there is always a one tick lag.
            Physics.SyncTransforms();
        }

        public abstract ModeBase GetMode(Container session = null);

        protected static Ray GetRayForPlayer(Container player)
        {
            var camera = player.Require<CameraComponent>();
            float yaw = camera.yaw * Mathf.Deg2Rad, pitch = camera.pitch * Mathf.Deg2Rad;
            var direction = new Vector3(Mathf.Sin(yaw), -Mathf.Sin(pitch), Mathf.Cos(yaw));
            Vector3 position = player.Require<MoveComponent>().position + new Vector3 {y = 1.8f};
            var ray = new Ray(position, direction);
            Debug.DrawLine(position, position + direction * 10.0f, Color.blue, 5.0f);
            return ray;
        }

        public virtual Container GetPlayerFromId(int playerId) { throw new NotImplementedException(); }

        public virtual void Dispose()
        {
            IsDisposed = true;
            foreach (PlayerModifierDispatcherBehavior modifier in m_Modifier)
                modifier.Dispose();
            foreach (IPlayerContainerRenderer visual in m_Visuals)
                visual.Dispose();
            Cursor.lockState = CursorLockMode.Confined;
            Cursor.visible = true;
        }
    }
}