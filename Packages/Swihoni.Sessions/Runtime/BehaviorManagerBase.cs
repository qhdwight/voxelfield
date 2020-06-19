using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions.Entities
{
    public abstract class VisualBehaviorBase : MonoBehaviour
    {
        private MeshRenderer[] m_Renders;
        
        public int id;

        internal virtual void Setup(IBehaviorManager behaviorManager) => m_Renders = GetComponentsInChildren<MeshRenderer>();

        public virtual void SetVisible(bool isVisible)
        {
            foreach (MeshRenderer render in m_Renders)
                render.enabled = isVisible;
        }

        public virtual void Render(Container container) { }
    }

    public abstract class ModifierBehaviorBase : MonoBehaviour
    {
        public int id;

        public virtual void SetActive(bool isActive) => gameObject.SetActive(isActive);

        public virtual void Modify(SessionBase session, Container container, uint timeUs, uint durationUs) { }
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

        public ModifierBehaviorBase[] Modifiers { get; }

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
        
        public void Setup(SessionBase session)
        {
            m_Session = session;
        }

        private void ObtainVisual(int id, int index)
        {
            Pool<VisualBehaviorBase> pool = m_VisualsPool[id - 1];
            VisualBehaviorBase visual = pool.Obtain();
            visual.Setup(this);
            visual.SetVisible(true);
            m_Visuals[index] = visual;
        }

        private void ReturnVisual(int index)
        {
            VisualBehaviorBase visual = m_Visuals[index];
            if (!visual) return;
            Pool<VisualBehaviorBase> pool = m_VisualsPool[visual.id - 1];
            visual.SetVisible(false);
            visual.transform.SetParent(null, false);
            pool.Return(visual);
            m_Visuals[index] = null;
        }

        public abstract ArrayElementBase ExtractArray(Container session);

        public ModifierBehaviorBase ObtainModifier(Container session, int id)
        {
            void ObtainModifierAtIndex(Container container, int index, ModifierBehaviorBase modifierBehavior)
            {
                container.Zero();
                if (container.With(out ThrowableComponent throwable)) throwable.popTimeUs.Value = uint.MaxValue;
                container.Require<IdProperty>().Value = (byte) id;
                Modifiers[index] = modifierBehavior;
            }
            Pool<ModifierBehaviorBase> pool = m_ModifiersPool[id - 1];
            ModifierBehaviorBase modifier = pool.Obtain();
            modifier.SetActive(true);
            ArrayElementBase array = ExtractArray(session);
            for (var index = 0; index < array.Length; index++)
            {
                var container = (Container) array.GetValue(index);
                if (container.Require<IdProperty>() != None) continue;
                /* Found empty slot */
                ObtainModifierAtIndex(container, index, modifier);
                return modifier;
            }
            /* Circle back to first. We ran out of spots */
            // TODO:refactor better way?
            ReturnModifier(0);
            ObtainModifierAtIndex((Container) array.GetValue(0), 0, modifier);
            return modifier;
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

        public void Modify(Container session, uint timeUs, uint durationUs)
        {
            ArrayElementBase elements = ExtractArray(session);
            for (var index = 0; index < elements.Length; index++)
            {
                var container = (Container) elements.GetValue(index);
                var id = container.Require<IdProperty>();
                if (id == None)
                    continue;
                Modifiers[index].Modify(m_Session, container, timeUs, durationUs);
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

        public void Render(ArrayElementBase array)
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
                m_Visuals[index].Render(container);
            }
        }
    }
}