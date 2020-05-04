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

        private EntityModifierBehavior[] m_Modifiers = new EntityModifierBehavior[10];

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

        public EntityVisualBehavior ObtainVisual(int entityId)
        {
            Pool<EntityVisualBehavior> pool = m_EntityVisualsPool[entityId - 1];
            EntityVisualBehavior visual = pool.Obtain();
            visual.Setup();
            visual.SetVisible(true);
            return visual;
        }

        public void ReturnVisual(EntityVisualBehavior visual)
        {
            Pool<EntityVisualBehavior> pool = m_EntityVisualsPool[visual.id - 1];
            visual.SetVisible(false);
            visual.transform.SetParent(null, false);
            pool.Return(visual);
        }

        public EntityModifierBehavior ObtainModifier(Container session, byte entityId)
        {
            Pool<EntityModifierBehavior> pool = m_EntityModifiersPool[entityId - 1];
            EntityModifierBehavior modifier = pool.Obtain();
            modifier.SetActive(true);
            var entities = session.Require<EntityArrayProperty>();
            foreach (EntityContainer entity in entities)
            {
                var id = entity.Require<EntityId>();
                if (id == EntityId.None)
                {
                    id.Value = entityId;
                    break;
                }
            }
            return modifier;
        }

        public void ReturnModifier(EntityModifierBehavior modifier)
        {
            Pool<EntityModifierBehavior> pool = m_EntityModifiersPool[modifier.id - 1];
            modifier.SetActive(false);
            pool.Return(modifier);
        }

        public void Modify(Container session)
        {
            var entities = session.Require<EntityArrayProperty>();
            foreach (EntityContainer entity in entities)
            {
                var id = entity.Require<EntityId>();
                if (id != EntityId.None)
                {
                    m_Modifiers[id - 1].Modify(entity);
                    break;
                }
            }
        }
    }
}