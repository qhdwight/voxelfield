using System;
using System.Linq;
using Collections;
using Components;
using Session.Components;
using Session.Player.Modifiers;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Session
{
    public interface IGameObjectLinker
    {
        (GameObject, GameObject) GetPlayerPrefabs();
    }

    public interface IPlayerContainerRenderer
    {
        void Render(ContainerBase playerContainer, bool isLocalPlayer);
    }

    public abstract class SessionBase : IDisposable
    {
        internal const int MaxPlayers = 2;
        
        public abstract void Start();

        public abstract void Update();

        public abstract void FixedUpdate();

        public virtual void Dispose()
        {
        }
    }

    public abstract class SessionBase<TSessionComponent> : SessionBase
        where TSessionComponent : SessionContainerBase
    {
        private readonly GameObject m_PlayerModifierPrefab, m_PlayerVisualsPrefab;

        internal readonly TSessionComponent m_EmptySessionComponent = Activator.CreateInstance<TSessionComponent>();

        private float m_FixedUpdateTime, m_RenderTime;
        protected PlayerModifierDispatcherBehavior[] m_Modifier;
        protected IPlayerContainerRenderer[] m_Visuals;
        protected SessionSettingsComponent m_Settings = DebugBehavior.Singleton.Settings;
        protected uint m_Tick;

        public bool ShouldRender { get; set; } = true;

        protected SessionBase(IGameObjectLinker linker)
        {
            (m_PlayerModifierPrefab, m_PlayerVisualsPrefab) = linker.GetPlayerPrefabs();
        }

        private static T[] Instantiate<T>(GameObject prefab, int length, Action<T> setup)
        {
            return Enumerable.Range(0, length).Select(i =>
            {
                GameObject instance = UnityObject.Instantiate(prefab);
                instance.name = $"{prefab.name} ({i})";
                var component = instance.GetComponent<T>();
                setup(component);
                return component;
            }).ToArray();
        }

        public override void Start()
        {
            m_Visuals = Instantiate<IPlayerContainerRenderer>(m_PlayerVisualsPrefab, MaxPlayers, visuals => { });
            m_Modifier = Instantiate<PlayerModifierDispatcherBehavior>(m_PlayerModifierPrefab, MaxPlayers, visuals => visuals.Setup());
        }

        public override void Update()
        {
            float time = Time.realtimeSinceStartup, delta = time - m_RenderTime;
            Input(delta);
            if (ShouldRender) Render(delta, time - m_FixedUpdateTime, time);
            m_RenderTime = time;
        }

        protected virtual void Render(float renderDelta, float timeSinceTick, float renderTime)
        {
        }

        protected virtual void Tick(uint tick, float time)
        {
            Time.fixedDeltaTime = 1.0f / m_Settings.tickRate;
        }

        public virtual void Input(float delta)
        {
        }

        public override void FixedUpdate()
        {
            Tick(m_Tick++, m_FixedUpdateTime = Time.realtimeSinceStartup);
        }

        protected void InterpolateHistoryInto<TComponent>(TComponent componentToInterpolate, CyclicArray<TComponent> componentHistory,
                                                          Func<TComponent, float> getDuration, float rollback, float timeSinceLastUpdate)
            where TComponent : ComponentBase
        {
            InterpolateHistoryInto(componentToInterpolate, i => componentHistory.Get(i), componentHistory.Size, getDuration, rollback, timeSinceLastUpdate);
        }

        protected static void InterpolateHistoryInto<TComponent>(TComponent componentToInterpolate, Func<int, TComponent> getInHistory, int maxRollback,
                                                                 Func<TComponent, float> getDuration, float rollback, float timeSinceLastUpdate)
            where TComponent : ComponentBase
        {
            TComponent fromComponent = null, toComponent = null;
            var durationCount = 0.0f;
            for (var componentHistoryIndex = 0; componentHistoryIndex < maxRollback; componentHistoryIndex++)
            {
                fromComponent = getInHistory(-componentHistoryIndex - 1);
                toComponent = getInHistory(-componentHistoryIndex);
                durationCount += getDuration(toComponent);
                if (durationCount >= rollback - timeSinceLastUpdate) break;
                if (componentHistoryIndex != maxRollback - 1) continue;
                // We do not have enough history. Copy the most recent instead
                componentToInterpolate.MergeSet(getInHistory(0));
                return;
            }
            if (toComponent == null)
                throw new ArgumentException("Cyclic array is not big enough");
            float interpolation;
            if (getDuration(toComponent) > 0.0f)
            {
                float elapsed = durationCount - rollback + timeSinceLastUpdate;
                interpolation = elapsed / getDuration(toComponent);
            }
            else
                interpolation = 0.0f;
            Interpolator.InterpolateInto(fromComponent, toComponent, componentToInterpolate, interpolation);
        }

        protected void RenderSessionComponent(SessionContainerBase<ContainerBase> session)
        {
            for (var playerId = 0; playerId < session.playerComponents.Length; playerId++)
                m_Visuals[playerId].Render(session.playerComponents[playerId], playerId == session.localPlayerId);
        }
    }
}