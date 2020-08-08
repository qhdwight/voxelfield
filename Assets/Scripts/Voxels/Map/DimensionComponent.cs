using System;
using Swihoni.Components;
using Swihoni.Util.Math;

namespace Voxels.Map
{
    public struct Dimension
    {
        public Position3Int lowerBound, upperBound;

        public Dimension(in Position3Int lowerBound, in Position3Int upperBound)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }
    }

    [Serializable]
    public class DimensionComponent : ComponentBase
    {
        public Position3IntProperty lowerBound, upperBound;
    }
}