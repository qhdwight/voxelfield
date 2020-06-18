using Swihoni.Collections;
using Swihoni.Components;

namespace Swihoni.Sessions.Entities
{
    public class EntityManager : BehaviorManagerBase
    {
        internal static readonly Pool<EntityManager> Pool = new Pool<EntityManager>(1, () => new EntityManager(), UsageChanged);

        private static void UsageChanged(EntityManager manager, bool isActive)
        {
            if (!isActive)
                manager.SetAllInactive();
        }

        public override ArrayElementBase ExtractArray(Container session) => session.Require<EntityArrayElement>();
    }
}