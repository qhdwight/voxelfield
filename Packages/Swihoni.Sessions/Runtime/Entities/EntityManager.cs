using Swihoni.Collections;
using Swihoni.Components;
using UnityEngine;

namespace Swihoni.Sessions.Entities
{
    public class EntityManager : BehaviorManagerBase
    {
        internal static Pool<EntityManager> pool;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            pool = new Pool<EntityManager>(1, () => new EntityManager(), UsageChanged);
            Application.quitting -= Cleanup;
            Application.quitting += Cleanup;
        }

        private static void Cleanup() => pool.Dispose();

        private static void UsageChanged(EntityManager manager, bool isActive)
        {
            if (!isActive) manager.SetAllInactive();
        }

        private EntityManager() : base(EntityArray.Count, "Entities") { }

        public override ArrayElementBase ExtractArray(Container session) => session.Require<EntityArray>();
    }
}