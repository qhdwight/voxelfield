using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions
{
    public interface IPlayerContainerRenderer : IDisposable
    {
        void Setup();

        // TODO: refactor is local player should use If construct
        void Render(int playerId, Container player, bool isLocalPlayer);
    }

    public abstract class SessionBase : IDisposable
    {
        internal const int MaxPlayers = 3;

        private readonly GameObject m_PlayerModifierPrefab, m_PlayerVisualsPrefab;
        
        protected readonly PlayerHudBase m_PlayerHud;
        private float m_FixedUpdateTime, m_RenderTime;
        protected PlayerModifierDispatcherBehavior[] m_Modifier;
        protected IPlayerContainerRenderer[] m_Visuals;
        private uint m_Tick;

        public bool ShouldRender { get; set; } = true;

        protected SessionBase(IReadOnlyCollection<Type> sessionElements, IReadOnlyCollection<Type> playerElements, IReadOnlyCollection<Type> commandElements)
        {
            m_PlayerModifierPrefab = SessionGameObjectLinker.Singleton.PlayerModifierPrefab;
            m_PlayerVisualsPrefab = SessionGameObjectLinker.Singleton.PlayerVisualsPrefab;
            m_PlayerHud = PlayerHudBase.Singleton;
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
            m_Visuals = Instantiate<IPlayerContainerRenderer>(m_PlayerVisualsPrefab, MaxPlayers, visuals => visuals.Setup());
            m_Modifier = Instantiate<PlayerModifierDispatcherBehavior>(m_PlayerModifierPrefab, MaxPlayers, modifier => modifier.Setup());
        }

        public void Update(float time)
        {
            float delta = time - m_RenderTime;
            Input(time, delta);
            if (ShouldRender) Render(time);
            m_RenderTime = time;
        }

        protected virtual void Render(float renderTime) { }

        protected virtual void Tick(uint tick, float time, float duration) { }

        protected virtual void Input(float time, float delta) { }

        public void FixedUpdate(float time)
        {
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
            foreach (PlayerModifierDispatcherBehavior modifier in m_Modifier)
                if (modifier)
                    UnityObject.Destroy(modifier.gameObject);
            foreach (IPlayerContainerRenderer visual in m_Visuals)
                visual.Dispose();
        }

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
            renderPlayerContainer.CopyFrom(getInHistory(0));
        }

        public abstract Ray GetRayForPlayer(int holdingPlayer);

        public abstract void AboutToRaycast(int playerId);

        public abstract ModeBase GetMode(Container session = null);
    }
}