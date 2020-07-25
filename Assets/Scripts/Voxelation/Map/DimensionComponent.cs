using System;
using Swihoni.Components;

namespace Voxelation.Map
{
    [Serializable]
    public class DimensionComponent : ComponentBase
    {
        public Position3IntProperty lowerBound, upperBound;
    }
}