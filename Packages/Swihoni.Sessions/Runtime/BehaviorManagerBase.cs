using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Entities;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions
{
    public abstract class IdBehavior : MonoBehaviour
    {
        public int id;

        protected IBehaviorManager m_Manager;

        internal virtual void Setup(IBehaviorManager manager) => m_Manager = manager;
    }

    public abstract class VisualBehaviorBase : IdBehavior
    {
        private const float ExpirationDurationSeconds = 60.0f;

        private MeshRenderer[] m_Renders;
        private float m_SetupTime;

        public bool IsExpired => Time.time - m_SetupTime > ExpirationDurationSeconds;

        internal override void Setup(IBehaviorManager manager)
        {
            base.Setup(manager);
            m_SetupTime = Time.time;
            m_Renders = GetComponentsInChildren<MeshRenderer>();
        }

        protected virtual void SetVisible(bool isVisible)
        {
            foreach (MeshRenderer render in m_Renders)
                render.enabled = isVisible;
        }

        public virtual void SetActive(bool isActive) => SetVisible(isActive);
    }

    public abstract class ModifierBehaviorBase : IdBehavior
    {
        public int Index { get; protected set; }

        public virtual void SetActive(Scene scene, bool isActive, int index)
        {
            SceneManager.MoveGameObjectToScene(gameObject, scene);
            Index = index;
            gameObject.SetActive(isActive);
        }
    }

    public interface IBehaviorManager
    {
        ModifierBehaviorBase GetModifierPrefab(int id);
    }

    public abstract class BehaviorManagerBase : IBehaviorManager, IDisposable
    {
        protected readonly Dictionary<int, VisualBehaviorBase> m_VisualPrefabs;
        protected readonly Dictionary<int, ModifierBehaviorBase> m_ModifierPrefabs;
        private readonly Dictionary<int, Pool<VisualBehaviorBase>> m_VisualsPool;
        private readonly Dictionary<int, Pool<ModifierBehaviorBase>> m_ModifiersPool;
        protected readonly VisualBehaviorBase[] m_Visuals;
        private readonly ModifierBehaviorBase[] m_Modifiers;
        private SessionBase m_Session;

        // ReSharper disable once ConvertToAutoPropertyWhenPossible
        public ModifierBehaviorBase[] UnsafeModifiers => m_Modifiers;
        public VisualBehaviorBase[] UnsafeVisuals => m_Visuals;

        public ModifierBehaviorBase GetModifierPrefab(int id)
        {
            try
            {
                return m_ModifierPrefabs[id];
            }
            catch (KeyNotFoundException)
            {
                Debug.LogWarning($"Behavior modifier prefab not found for ID: {id}. Check your resources folder");
                throw;
            }
        }

        protected BehaviorManagerBase(int count, string resourceFolder)
        {
            m_ModifierPrefabs = Resources.LoadAll<ModifierBehaviorBase>(resourceFolder)
                                         .Where(modifier => modifier.id >= 0)
                                         .ToDictionary(modifier => modifier.id, modifier => modifier);
            m_ModifiersPool = m_ModifierPrefabs.Select(pair => new
            {
                id = pair.Key, pool = new Pool<ModifierBehaviorBase>(0, () =>
                {
                    ModifierBehaviorBase modifierInstance = UnityObject.Instantiate(pair.Value);
                    modifierInstance.Setup(this);
                    modifierInstance.name = pair.Value.name;
                    return modifierInstance;
                })
            }).ToDictionary(pair => pair.id, pair => pair.pool);
            m_VisualPrefabs = Resources.LoadAll<VisualBehaviorBase>(resourceFolder)
                                       .Where(visual => visual.id >= 0)
                                       .ToDictionary(visual => visual.id, visual => visual);
            m_VisualsPool = m_VisualPrefabs.Keys.Select(id => new
            {
                id, pool = new Pool<VisualBehaviorBase>(0, () => InstantiateVisual(id))
            }).ToDictionary(pair => pair.id, pair => pair.pool);
            m_Visuals = new VisualBehaviorBase[count];
            m_Modifiers = new ModifierBehaviorBase[count];
            Debug.Log($"[{GetType().Name}] Registered {m_ModifiersPool.Count} modifiers and {m_VisualsPool.Count} visuals");
        }

        private VisualBehaviorBase InstantiateVisual(int id)
        {
            VisualBehaviorBase prefabVisual = m_VisualPrefabs[id],
                               visualsInstance = UnityObject.Instantiate(prefabVisual);
            visualsInstance.Setup(this);
            visualsInstance.name = prefabVisual.name;
            return visualsInstance;
        }

        public void Setup(SessionBase session) => m_Session = session;

        private VisualBehaviorBase ObtainVisualAtIndex(int index, int id)
        {
            Pool<VisualBehaviorBase> pool = m_VisualsPool[id];
            VisualBehaviorBase visual = pool.Obtain();
            SceneManager.MoveGameObjectToScene(visual.gameObject, m_Session.Scene);
            if (visual.IsExpired)
            {
                visual.SetActive(false);
                visual = pool.RemoveAndObtain(visual);
// #if UNITY_EDITOR
//                 Debug.Log($"Removed expired visual with id: {id}");
// #endif
            }
            visual.SetActive(true);
            m_Visuals[index] = visual;
            return visual;
        }

        private void ReturnVisualAtIndex(int index)
        {
            VisualBehaviorBase visual = m_Visuals[index];
            if (!visual) return;
            Pool<VisualBehaviorBase> pool = m_VisualsPool[visual.id];
            SceneManager.MoveGameObjectToScene(visual.gameObject, SceneManager.GetActiveScene());
            visual.SetActive(false);
            visual.transform.SetParent(null, false);
            pool.Return(visual);
            m_Visuals[index] = null;
        }

        public abstract ArrayElementBase ExtractArray(Container session);

        public (ModifierBehaviorBase, Container) ObtainNextModifier(Container session, int id)
        {
            static void SetupModifier(Container container)
            {
                container.Zero();
                if (container.With(out ThrowableComponent throwable)) throwable.popTimeUs.Value = uint.MaxValue;
            }
            ArrayElementBase array = ExtractArray(session);
            for (var index = 0; index < array.Length; index++)
            {
                var container = (Container) array[index];
                var idProperty = container.Require<ByteIdProperty>();
                if (idProperty.WithValue) continue;
                /* Found empty slot */
                SetupModifier(container);
                idProperty.Value = (byte) id;
                return (ObtainModifierAtIndex(container, index, id), container);
            }
            /* Circle back to first. We ran out of spots */
            // TODO:refactor better way?
            {
                ReturnModifierAtIndex(0);
                var container = (Container) array[0];
                SetupModifier(container);
                container.Require<ByteIdProperty>().Value = (byte) id;
                return (ObtainModifierAtIndex(container, 0, id), container);
            }
        }

        public ModifierBehaviorBase GetModifierAtIndex(Container container, int index, out bool isNewlyObtained)
        {
            var id = container.Require<ByteIdProperty>();
            if (id.WithoutValue)
            {
                ReturnModifierAtIndex(index);
                isNewlyObtained = false;
                return null;
            }
            ModifierBehaviorBase existing = m_Modifiers[index];
            isNewlyObtained = !existing || existing.id != id;
            return isNewlyObtained ? ObtainModifierAtIndex(container, index, id) : existing;
        }

        public VisualBehaviorBase GetVisualAtIndex(Container container, int index)
        {
            var id = container.Require<ByteIdProperty>();
            if (id.WithoutValue)
            {
                ReturnVisualAtIndex(index);
                return null;
            }
            VisualBehaviorBase existing = m_Visuals[index];
            bool useExisting = existing && existing.id == id;
            return useExisting ? existing : ObtainVisualAtIndex(index, id);
        }

        public ModifierBehaviorBase ObtainModifierAtIndex(Container container, int index, int id)
        {
            Pool<ModifierBehaviorBase> pool = m_ModifiersPool[id];
            ModifierBehaviorBase modifierBehavior = pool.Obtain();
            modifierBehavior.SetActive(m_Session.Scene, true, index);
            m_Modifiers[index] = modifierBehavior;
            return modifierBehavior;
        }

        private void ReturnModifierAtIndex(int index)
        {
            ModifierBehaviorBase modifier = m_Modifiers[index];
            if (!modifier) return;
            Pool<ModifierBehaviorBase> pool = m_ModifiersPool[modifier.id];
            modifier.SetActive(SceneManager.GetActiveScene(), false, index);
            pool.Return(modifier);
            m_Modifiers[index] = null;
        }

        // public void Modify(int index, Container session, Action<ModifierBehaviorBase, Container> action)
        // {
        //     var element = (Container) ExtractArray(session).GetValue(index);
        //     action(m_Modifiers[index], element);
        // }

        public void ModifyAll(Container session, Action<ModifierBehaviorBase, int, Container> iterate)
        {
            ArrayElementBase elements = ExtractArray(session);
            for (var index = 0; index < elements.Length; index++)
            {
                var containerElement = (Container) elements[index];
                var id = containerElement.Require<ByteIdProperty>();
                if (id.WithValue)
                    iterate(m_Modifiers[index], index, containerElement);
                // Remove modifier if lifetime has ended
                if (id.WithoutValue) ReturnModifierAtIndex(index);
            }
        }

        public void SetAllInactive()
        {
            if (m_Modifiers != null)
                for (var i = 0; i < m_Modifiers.Length; i++)
                    ReturnModifierAtIndex(i);
            if (m_Visuals != null)
                for (var i = 0; i < m_Visuals.Length; i++)
                    ReturnVisualAtIndex(i);
        }

        public void RenderAll(ArrayElementBase array, Action<VisualBehaviorBase, Container> iterate)
        {
            for (var index = 0; index < array.Length; index++)
            {
                var container = (Container) array[index];
                var id = container.Require<ByteIdProperty>();
                if (id.WithoutValue)
                {
                    ReturnVisualAtIndex(index);
                    continue;
                }
                if (!m_Visuals[index]) ObtainVisualAtIndex(index, id);
                iterate(m_Visuals[index], container);
            }
        }

        public void Dispose()
        {
            foreach (Pool<ModifierBehaviorBase> pool in m_ModifiersPool.Values)
                pool.Dispose();
            foreach (Pool<VisualBehaviorBase> pool in m_VisualsPool.Values)
                pool.Dispose();
        }
    }
}