using System;
using Swihoni.Components;

namespace Voxel.Map
{
    [Serializable]
    public class DimensionComponent : ComponentBase
    {
        public Position3IntProperty lowerBound, upperBound;
    }
}