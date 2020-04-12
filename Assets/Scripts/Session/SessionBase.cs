using System;
using System.Collections.Generic;
using System.Linq;
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
        // TODO: is local player should use With construct
        void Render(Container playerContainer, bool isLocalPlayer);
    }

    public abstract class SessionBase : IDisposable
    {
        internal const int MaxPlayers = 2;

        private readonly GameObject m_PlayerModifierPrefab, m_PlayerVisualsPrefab;

        private float m_FixedUpdateTime, m_RenderTime;
        protected PlayerModifierDispatcherBehavior[] m_Modifier;
        protected IPlayerContainerRenderer[] m_Visuals;
        protected SessionSettingsComponent m_Settings = DebugBehavior.Singleton.Settings;
        protected uint m_Tick;

        public bool ShouldRender { get; set; } = true;

        protected SessionBase(IGameObjectLinker linker, IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements,
                              IReadOnlyCollection<Type> commandElements)
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

        public virtual void Start()
        {
            m_Visuals = Instantiate<IPlayerContainerRenderer>(m_PlayerVisualsPrefab, MaxPlayers, visuals => { });
            m_Modifier = Instantiate<PlayerModifierDispatcherBehavior>(m_PlayerModifierPrefab, MaxPlayers, visuals => visuals.Setup());
        }

        public void Update()
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

        public void FixedUpdate()
        {
            Tick(m_Tick++, m_FixedUpdateTime = Time.realtimeSinceStartup);
        }

        // protected void InterpolateHistoryInto<TComponent>(TComponent componentToInterpolate, CyclicArray<TComponent> componentHistory,
        //                                                   Func<TComponent, float> getDuration, float rollback, float timeSinceLastUpdate)
        //     where TComponent : ComponentBase
        // {
        //     InterpolateHistoryInto(componentToInterpolate, i => componentHistory.Get(i), componentHistory.Size, getDuration, rollback, timeSinceLastUpdate);
        // }

        protected static void InterpolateHistoryInto<TComponent>(TComponent componentToInterpolate, Func<int, TComponent> getInHistory, int maxRollback,
                                                                 Func<int, float> getDuration, float rollback, float timeSinceLastUpdate)
            where TComponent : ComponentBase
        {
            int fromIndex = 0, toIndex = 0;
            var durationCount = 0.0f;
            for (var historyIndex = 0; historyIndex < maxRollback; historyIndex++)
            {
                fromIndex = -historyIndex - 1;
                toIndex = -historyIndex;
                durationCount += getDuration(-historyIndex);
                if (durationCount >= rollback - timeSinceLastUpdate) break;
                if (historyIndex != maxRollback - 1) continue;
                // We do not have enough history. Copy the most recent instead
                componentToInterpolate.MergeSet(getInHistory(0));
                return;
            }
            float interpolation;
            if (getDuration(toIndex) > 0.0f)
            {
                float elapsed = durationCount - rollback + timeSinceLastUpdate;
                interpolation = elapsed / getDuration(toIndex);
            }
            else
                interpolation = 0.0f;
            Interpolator.InterpolateInto(getInHistory(fromIndex), getInHistory(toIndex), componentToInterpolate, interpolation);
        }

        public virtual void Dispose()
        {
        }
    }
}