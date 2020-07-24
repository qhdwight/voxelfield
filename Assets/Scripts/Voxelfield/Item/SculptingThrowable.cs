using Swihoni.Sessions;
using Swihoni.Sessions.Entities;
using Swihoni.Util.Math;
using Voxel;
using Voxelfield.Session;

namespace Voxelfield.Item
{
    public class SculptingThrowable : ThrowableModifierBehavior
    {
        protected override void JustPopped(in ModifyContext context)
        {
            if (context.session.Injector is Injector injector)
            {
                var center = (Position3Int) transform.position;
                injector.EvaluateVoxelChange(center, new VoxelChange());
            }
        }
    }
}