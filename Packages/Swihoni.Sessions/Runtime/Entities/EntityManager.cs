using System.Linq;
using Swihoni.Collections;
using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityManager
    {
        private Pool<EntityModifierBehavior>[] m_EntityModifiersPool;
        private Pool<EntityVisualBehavior>[] m_EntityVisualsPool;

        private readonly EntityModifierBehavior[] m_Modifiers = new EntityModifierBehavior[10];
        private readonly EntityVisualBehavior[] m_Visuals = new EntityVisualBehavior[10];

        public void Setup()
        {
            m_EntityModifiersPool = Resources.LoadAll<EntityModifierBehavior>("Entities")
                                             .OrderBy(modifier => modifier.id)
                                             .Select(prefabModifier => new Pool<EntityModifierBehavior>(0, () =>
                                              {
                                                  EntityModifierBehavior visualsInstance = Object.Instantiate(prefabModifier);
                                                  visualsInstance.name = prefabModifier.name;
                                                  return visualsInstance;
                                              })).ToArray();
            m_EntityVisualsPool = Resources.LoadAll<EntityVisualBehavior>("Entities")
                                           .OrderBy(visuals => visuals.id)
                                           .Select(prefabVisual => new Pool<EntityVisualBehavior>(0, () =>
                                            {
                                                EntityVisualBehavior visualsInstance = Object.Instantiate(prefabVisual);
                                                visualsInstance.name = prefabVisual.name;
                                                return visualsInstance;
                                            })).ToArray();
        }

        private EntityVisualBehavior ObtainVisual(int entityId, int index)
        {
            Pool<EntityVisualBehavior> pool = m_EntityVisualsPool[entityId - 1];
            EntityVisualBehavior visual = pool.Obtain();
            visual.Setup();
            visual.SetVisible(true);
            m_Visuals[index] = visual;
            return visual;
        }

        private void ReturnVisual(int index)
        {
            EntityVisualBehavior visual = m_Visuals[index];
            Pool<EntityVisualBehavior> pool = m_EntityVisualsPool[visual.id - 1];
            visual.SetVisible(false);
            visual.transform.SetParent(null, false);
            pool.Return(visual);
            m_Visuals[index] = null;
        }

        public EntityModifierBehavior ObtainModifier(Container session, byte entityId)
        {
            Pool<EntityModifierBehavior> pool = m_EntityModifiersPool[entityId - 1];
            EntityModifierBehavior modifier = pool.Obtain();
            modifier.SetActive(true);
            var entities = session.Require<EntityArrayProperty>();
            for (var index = 0; index < entities.Length; index++)
            {
                EntityContainer entity = entities[index];
                if (entity.id != EntityId.None) continue;
                /* Found empty slot */
                entity.Zero();
                entity.id.Value = entityId;
                m_Modifiers[index] = modifier;
                return modifier;
            }
            /* Circle back to first. We ran out of entities */
            // TODO:refactor better way?
            ReturnModifier(0);
            entities[0].Zero();
            entities[0].id.Value = entityId;
            m_Modifiers[0] = modifier;
            return modifier;
        }

        private void ReturnModifier(int index)
        {
            EntityModifierBehavior modifier = m_Modifiers[index];
            Pool<EntityModifierBehavior> pool = m_EntityModifiersPool[modifier.id - 1];
            modifier.SetActive(false);
            pool.Return(modifier);
            m_Modifiers[index] = null;
        }

        public void Modify(Container session, float duration)
        {
            var entities = session.Require<EntityArrayProperty>();
            for (var index = 0; index < entities.Length; index++)
            {
                EntityContainer entity = entities[index];
                if (entity.id == EntityId.None)
                    continue;
                m_Modifiers[index].Modify(entity, duration);
                // Remove modifier if lifetime has ended
                if (entity.id == EntityId.None && m_Modifiers[index] != null) ReturnModifier(index);
            }
        }

        public void Render(EntityArrayProperty entities)
        {
            for (var index = 0; index < entities.Length; index++)
            {
                EntityContainer entity = entities[index];
                if (entity.id == EntityId.None)
                {
                    if (m_Visuals[index] != null) ReturnVisual(index);
                    continue;
                }
                if (m_Visuals[index] == null) ObtainVisual(entity.id, index);
                m_Visuals[index].Render(entity);
            }
        }
    }
}