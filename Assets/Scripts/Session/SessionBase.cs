using System;
using System.Collections.Generic;
using System.Linq;
using Collections;
using Components;
using Session.Player;
using Session.Player.Components;
using Session.Player.Modifiers;
using Session.Player.Visualization;
using UnityEngine;
using Object = UnityEngine.Object;

namespace Session
{
    public interface IGameObjectLinker
    {
        (GameObject, GameObject) GetPlayerPrefabs();
    }

    public abstract class SessionBase
    {
        internal const int MaxPlayers = 2;

        public static readonly Dictionary<Type, byte> TypeToId = new Dictionary<Type, byte>
        {
            [typeof(PingCheckComponent)] = 0,
            [typeof(ClientCommandComponent)] = 1
        };
    }

    public abstract class SessionBase<TSessionComponent> : SessionBase, IDisposable
        where TSessionComponent : SessionComponentBase
    {
        private readonly GameObject m_PlayerModifierPrefab, m_PlayerVisualsPrefab;

        private float m_FixedUpdateTime, m_RenderTime;

        protected PlayerModifierDispatcherBehavior[] m_Modifier;
        protected PlayerVisualsDispatcherBehavior[] m_Visuals;
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
                GameObject instance = Object.Instantiate(prefab);
                instance.name = $"{prefab.name} ({i})";
                var component = instance.GetComponent<T>();
                setup(component);
                return component;
            }).ToArray();
        }

        public virtual void Start()
        {
            m_Visuals = Instantiate<PlayerVisualsDispatcherBehavior>(m_PlayerVisualsPrefab, MaxPlayers, visuals => visuals.Setup());
            m_Modifier = Instantiate<PlayerModifierDispatcherBehavior>(m_PlayerModifierPrefab, MaxPlayers, visuals => visuals.Setup());
        }

        public void Update()
        {
            float time = Time.realtimeSinceStartup, delta = time - m_RenderTime;
            Input(delta);
            if (ShouldRender) Render(delta, time - m_FixedUpdateTime);
            m_RenderTime = time;
        }

        protected virtual void Render(float renderDelta, float timeSinceTick)
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

        protected bool InterpolateHistoryInto<T>(ComponentBase componentToInterpolate, CyclicArray<T> componentHistory,
                                                 Func<T, float> getDuration, float rollback, float timeSinceLastUpdate) where T : ComponentBase
        {
            T fromComponent = null, toComponent = null;
            var durationCount = 0.0f;
            for (var componentHistoryIndex = 0; componentHistoryIndex < componentHistory.Size; componentHistoryIndex++)
            {
                fromComponent = componentHistory.Get(-componentHistoryIndex - 1);
                toComponent = componentHistory.Get(-componentHistoryIndex);
                durationCount += getDuration(toComponent);
                if (durationCount >= rollback - timeSinceLastUpdate) break;
                if (componentHistoryIndex != componentHistory.Size - 1) continue;
                // We do not have enough history. Copy the most recent instead
                Copier.MergeSet(componentToInterpolate, componentHistory.Peek());
                return false;
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
            {
                interpolation = 0.0f;
            }
            Interpolator.InterpolateInto(fromComponent, toComponent, componentToInterpolate, interpolation);
            return true;
        }

        protected void RenderSessionComponent(SessionComponentBase session)
        {
            SceneCamera.Singleton.SetEnabled(!session.localPlayerId.HasValue || session.LocalPlayerComponent.IsDead);
            for (var playerId = 0; playerId < session.playerComponents.Length; playerId++)
                m_Visuals[playerId].Visualize(session.playerComponents[playerId], playerId == session.localPlayerId);
        }

        public virtual void Dispose()
        {
        }
    }

    [Serializable]
    public class PingCheckComponent : ComponentBase
    {
        public UIntProperty tick;
    }

    [Serializable]
    public class StampedPlayerComponent : PlayerComponent
    {
        public StampComponent stamp;
    }

    [Serializable]
    public class ClientCommandComponent : PlayerCommandsComponent
    {
        public StampComponent stamp;
        public PlayerComponent trustedComponent;
    }
}