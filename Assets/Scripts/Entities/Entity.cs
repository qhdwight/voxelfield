using Unity.Entities;
using Unity.Jobs;

namespace Entities
{
    public class EntitySystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps)
        {
            return inputDeps;
        }
    }
    
    public struct EntityData : IComponentData
    {
        
    }
}