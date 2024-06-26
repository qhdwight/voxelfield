using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;
using Voxels.Map;

namespace Voxelfield
{
    [SelectionBase]
    public class ModelBehavior : ModelBehaviorBase
    {
        public static bool IsModeOrDesigner(Container container, int modeId)
        {
            var mode = container.Require<ModeIdProperty>();
            return mode == modeId || mode == ModeIdProperty.Designer;
        }

        public override void SetInMode(Container session) => gameObject.SetActive(true);
    }
}