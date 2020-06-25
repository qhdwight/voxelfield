using System;
using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Entities;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions
{
    public abstract class IdBehavior : MonoBehaviour
    {
        public int id;

        internal virtual void Setup(IBehaviorManager behaviorManager) { }
    }
    
    public abstract class VisualBehaviorBase : IdBehavior
    {
        private MeshRenderer[] m_Renders;

        internal override void Setup(IBehaviorManager behaviorManager)
        {
            base.Setup(behaviorManager);
            m_Renders = GetComponentsInChildren<MeshRenderer>();
        }

        protected void SetVisible(bool isVisible)
        {
            foreach (MeshRenderer render in m_Renders)
                render.enabled = isVisible;  
        }

        public virtual void SetActive(bool isActive) => SetVisible(isActive);
    }

    public abstract class ModifierBehaviorBase : IdBehavior
    {
        public virtual void SetActive(bool isActive) => gameObject.SetActive(isActive);
    }

    public interface IBehaviorManager
    {
        ModifierBehaviorBase GetModifierPrefab(int id);
    }

    public abstract class BehaviorManagerBase : IBehaviorManager
    {
        private const byte None = 0;

        protected readonly VisualBehaviorBase[] m_Visuals;
        protected readonly ModifierBehaviorBase[] m_ModifierPrefabs;
        private readonly Pool<VisualBehaviorBase>[] m_VisualsPool;
        private readonly Pool<ModifierBehaviorBase>[] m_ModifiersPool;
        private SessionBase m_Session;

        private ModifierBehaviorBase[] Modifiers { get; }

        protected BehaviorManagerBase(int count, string resourceFolder)
        {
            m_ModifierPrefabs = Resources.LoadAll<ModifierBehaviorBase>(resourceFolder)
                                         .OrderBy(modifier => modifier.id).ToArray();
            m_ModifiersPool = m_ModifierPrefabs
                             .Select(prefabModifier => new Pool<ModifierBehaviorBase>(0, () =>
                              {
                                  ModifierBehaviorBase visualsInstance = UnityObject.Instantiate(prefabModifier);
                                  visualsInstance.name = prefabModifier.name;
                                  return visualsInstance;
                              })).ToArray();
            m_VisualsPool = Resources.LoadAll<VisualBehaviorBase>(resourceFolder)
                                     .OrderBy(visuals => visuals.id)
                                     .Select(prefabVisual => new Pool<VisualBehaviorBase>(0, () =>
                                      {
                                          VisualBehaviorBase visualsInstance = UnityObject.Instantiate(prefabVisual);
                                          visualsInstance.name = prefabVisual.name;
                                          return visualsInstance;
                                      })).ToArray();
            m_Visuals = new VisualBehaviorBase[count];
            Modifiers = new ModifierBehaviorBase[count];
        }
        
        public void Setup(SessionBase session) => m_Session = session;

        private void ObtainVisual(int id, int index)
        {
            Pool<VisualBehaviorBase> pool = m_VisualsPool[id - 1];
            VisualBehaviorBase visual = pool.Obtain();
            visual.Setup(this);
            visual.SetActive(true);
            m_Visuals[index] = visual;
        }

        private void ReturnVisual(int index)
        {
            VisualBehaviorBase visual = m_Visuals[index];
            if (!visual) return;
            Pool<VisualBehaviorBase> pool = m_VisualsPool[visual.id - 1];
            visual.SetActive(false);
            visual.transform.SetParent(null, false);
            pool.Return(visual);
            m_Visuals[index] = null;
        }

        public abstract ArrayElementBase ExtractArray(Container session);

        public ModifierBehaviorBase ObtainNextModifier(Container session, int id)
        {
            ArrayElementBase array = ExtractArray(session);
            for (var index = 0; index < array.Length; index++)
            {
                var container = (Container) array.GetValue(index);
                if (container.Require<IdProperty>() != None) continue;
                /* Found empty slot */
                return ObtainModifierAtIndex(container, index, id);
            }
            /* Circle back to first. We ran out of spots */
            // TODO:refactor better way?
            ReturnModifier(0);
            return ObtainModifierAtIndex((Container) array.GetValue(0), 0, id);
        }

        public ModifierBehaviorBase ObtainModifierAtIndex(Container container, int index, int id)
        {
            Pool<ModifierBehaviorBase> pool = m_ModifiersPool[id - 1];
            ModifierBehaviorBase modifierBehavior = pool.Obtain();
            modifierBehavior.SetActive(true);
            container.Zero();
            if (container.With(out ThrowableComponent throwable)) throwable.popTimeUs.Value = uint.MaxValue;
            container.Require<IdProperty>().Value = (byte) id;
            Modifiers[index] = modifierBehavior;
            return modifierBehavior;
        }

        private void ReturnModifier(int index)
        {
            ModifierBehaviorBase modifier = Modifiers[index];
            if (!modifier) return;
            Pool<ModifierBehaviorBase> pool = m_ModifiersPool[modifier.id - 1];
            modifier.SetActive(false);
            pool.Return(modifier);
            Modifiers[index] = null;
        }

        public ModifierBehaviorBase GetModifierPrefab(int id) => m_ModifierPrefabs[id - 1];

        public void Modify(int index, Container session, Action<ModifierBehaviorBase, Container> action)
        {
            var element = (Container) ExtractArray(session).GetValue(index);
            action(Modifiers[index], element);
        }
        
        public void ModifyAll(Container session, Action<ModifierBehaviorBase, int, Container> iterate)
        {
            ArrayElementBase elements = ExtractArray(session);
            for (var index = 0; index < elements.Length; index++)
            {
                var container = (Container) elements.GetValue(index);
                var id = container.Require<IdProperty>();
                if (id == None)
                    continue;
                iterate(Modifiers[index], index, container);
                // Remove modifier if lifetime has ended
                if (id == None && Modifiers[index] != null) ReturnModifier(index);
            }
        }

        public void SetAllInactive()
        {
            if (Modifiers != null)
                for (var i = 0; i < Modifiers.Length; i++)
                    ReturnModifier(i);
            if (m_Visuals != null)
                for (var i = 0; i < m_Visuals.Length; i++)
                    ReturnVisual(i);
        }

        public void RenderAll(ArrayElementBase array, Action<VisualBehaviorBase, Container> iterate)
        {
            for (var index = 0; index < array.Length; index++)
            {
                var container = (Container) array.GetValue(index);
                byte id = container.Require<IdProperty>();
                if (id == None)
                {
                    if (m_Visuals[index] != null) ReturnVisual(index);
                    continue;
                }
                if (m_Visuals[index] == null) ObtainVisual(id, index);
                iterate(m_Visuals[index], container);
            }
        }
    }
}