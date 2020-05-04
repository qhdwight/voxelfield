using System.Linq;
using Swihoni.Collections;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public static class EntityAssetLink
    {
        private static Pool<EntityModifierBehavior>[] _entityModifiersPool;
        private static Pool<EntityVisualBehavior>[] _entityVisualsPool;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            _entityModifiersPool = Resources.LoadAll<EntityModifierBehavior>("Entities")
                                            .OrderBy(modifier => modifier.id)
                                            .Select(prefabModifier => new Pool<EntityModifierBehavior>(0, () =>
                                             {
                                                 EntityModifierBehavior visualsInstance = Object.Instantiate(prefabModifier);
                                                 visualsInstance.name = prefabModifier.name;
                                                 return visualsInstance;
                                             })).ToArray();
            _entityVisualsPool = Resources.LoadAll<EntityVisualBehavior>("Entities")
                                          .OrderBy(visuals => visuals.id)
                                          .Select(prefabVisual => new Pool<EntityVisualBehavior>(0, () =>
                                           {
                                               EntityVisualBehavior visualsInstance = Object.Instantiate(prefabVisual);
                                               visualsInstance.name = prefabVisual.name;
                                               return visualsInstance;
                                           })).ToArray();
        }

        public static EntityVisualBehavior ObtainVisual(int entityId)
        {
            Pool<EntityVisualBehavior> pool = _entityVisualsPool[entityId - 1];
            EntityVisualBehavior visual = pool.Obtain();
            visual.Setup();
            visual.SetVisible(true);
            return visual;
        }

        public static void ReturnVisual(EntityVisualBehavior visual)
        {
            Pool<EntityVisualBehavior> pool = _entityVisualsPool[visual.id - 1];
            visual.SetVisible(false);
            visual.transform.SetParent(null, false);
            pool.Return(visual);
        }

        public static EntityModifierBehavior ObtainModifier(int entityId)
        {
            Pool<EntityModifierBehavior> pool = _entityModifiersPool[entityId - 1];
            EntityModifierBehavior modifier = pool.Obtain();
            modifier.SetActive(true);
            return modifier;
        }

        public static void ReturnModifier(EntityModifierBehavior modifier)
        {
            Pool<EntityModifierBehavior> pool = _entityModifiersPool[modifier.id - 1];
            modifier.SetActive(false);
            pool.Return(modifier);
        }
    }
}