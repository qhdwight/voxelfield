using Unity.Entities;

namespace Entities
{
    public class MoveSystem : SystemBase
    {
        protected override void OnUpdate()
        {
            Entities.ForEach((ref PlayerComponent playerComponent) =>
            {
                
            }).Run();
        }
    }
}