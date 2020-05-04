using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using Swihoni.Sessions.Entities;
using UnityEngine;
using UnityObject = UnityEngine.Object;

namespace Swihoni.Sessions.Mapper
{
    public interface IIdentifiable
    {
        int GetId();
    }

    public interface IElementModifier<in TElement, in TCommands> : IDisposable
    {
        void Setup(SessionBase session);

        void Modify(SessionBase session, Container root, int index, TElement element, TCommands commands, float duration);

        void Synchronize(TElement element);

        void ModifyCommands(SessionBase session, TCommands commands);

        void SetActive(bool isActive);
    }

    public interface IElementVisuals<TElement> : IDisposable
    {
        void Setup(SessionBase session);

        void Render(Container root, int index, TElement player);

        TElement GetLatestRendered();

        void SetActive(bool isActive);
    }

    public class MeshedElementVisualsBase<TElement> : MonoBehaviour, IElementVisuals<TElement>
    {
        private Renderer[] m_Renderers;
        private TElement m_Latest;

        public void Dispose() => Destroy(gameObject);

        public void Setup(SessionBase session) { m_Renderers = GetComponentsInChildren<Renderer>(); }

        public virtual void Render(Container root, int index, TElement player) => m_Latest = player;

        public TElement GetLatestRendered() => m_Latest;

        public void SetActive(bool isActive)
        {
            foreach (Renderer meshRenderer in m_Renderers)
                meshRenderer.enabled = isActive;
        }
    }

    public abstract class BehaviorElementModifier : MonoBehaviour, IElementModifier<EntityContainer, Container>, IIdentifiable
    {
        [SerializeField] private int m_Id;

        public virtual void Dispose() => Destroy(gameObject);

        public virtual void Setup(SessionBase session) => SetActive(false);

        public abstract void Modify(SessionBase session, Container root, int index, EntityContainer element, Container commands, float duration);

        public abstract void Synchronize(EntityContainer element);

        public abstract void ModifyCommands(SessionBase session, Container commands);

        public virtual void SetActive(bool isActive) => gameObject.SetActive(isActive);

        public virtual int GetId() => m_Id;
    }

    public abstract class UnityObjectMapperModifier<TElement, TCommands> : IDisposable
        where TElement : ElementBase, new()
        where TCommands : ElementBase, new()
    {
        private IElementModifier<TElement, TCommands>[] m_Modifiers;
        private IElementVisuals<TElement>[] m_Visuals;

        public UnityObjectMapperModifier() { }

        protected static Pool<TBehaviour>[] LoadComponentPoolFromResource<TBehaviour>(string directory) where TBehaviour : MonoBehaviour, IIdentifiable =>
            Resources.LoadAll<TBehaviour>(directory)
                     .OrderBy(visuals => visuals.GetId())
                     .Select(prefabBehavior => new Pool<TBehaviour>(0, () =>
                      {
                          GameObject visualsInstance = UnityObject.Instantiate(prefabBehavior.gameObject);
                          visualsInstance.name = prefabBehavior.gameObject.name;
                          return visualsInstance.GetComponent<TBehaviour>();
                      })).ToArray();

        // public static T GetInterface<T>(GameObject gameObject)
        // {
        //     foreach (Component component in gameObject.GetComponents<Component>())
        //     {
        //         if (component is T @interface)
        //             return @interface;
        //     }
        //     return default;
        // }

        void Modify(SessionBase session, Container root, int index, ArrayProperty<TElement> elements, TCommands commands, float duration)
        {
            if (m_Modifiers == null) m_Modifiers = new IElementModifier<TElement, TCommands>[elements.Length];
            IElementModifier<TElement, TCommands> currentModifier = m_Modifiers[index], modifier = SynchronizeModifier(index, elements[index]);
            m_Modifiers[index] = modifier;
            if (currentModifier != modifier) SwitchModifier(currentModifier, modifier);
            modifier?.Modify(session, root, index, elements[index], commands, duration);
        }

        void ModifyCommands(SessionBase session, int index, TCommands commands) { m_Modifiers[index].ModifyCommands(session, commands); }

        void Render(int index, Container root, ArrayProperty<TElement> elements)
        {
            if (m_Visuals == null) m_Visuals = new IElementVisuals<TElement>[elements.Length];
            IElementVisuals<TElement> currentVisuals = m_Visuals[index], visuals = SynchronizeVisuals(index, elements[index]);
            m_Visuals[index] = visuals;
            if (currentVisuals != visuals) SwitchVisuals(currentVisuals, visuals);
            visuals?.Render(root, index, elements[index]);
        }

        protected virtual void SwitchModifier(IElementModifier<TElement, TCommands> oldModifier, IElementModifier<TElement, TCommands> newModifier)
        {
            oldModifier?.SetActive(false);
            newModifier?.SetActive(true);
        }

        protected virtual void SwitchVisuals(IElementVisuals<TElement> oldVisuals, IElementVisuals<TElement> newVisuals)
        {
            oldVisuals?.SetActive(false);
            newVisuals?.SetActive(true);
        }

        protected abstract IElementModifier<TElement, TCommands> SynchronizeModifier(int index, TElement element);

        protected abstract IElementVisuals<TElement> SynchronizeVisuals(int index, TElement element);

        public void Dispose()
        {
            foreach (IElementModifier<TElement, TCommands> modifier in m_Modifiers) modifier.Dispose();
            foreach (IElementVisuals<TElement> visual in m_Visuals) visual.Dispose();
        }
    }
}