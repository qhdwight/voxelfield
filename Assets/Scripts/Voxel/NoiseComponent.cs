using System;
using Swihoni.Components;

namespace Voxel
{
    [Serializable]
    public class NoiseComponent : ComponentBase
    {
        public IntProperty seed;
        public ByteProperty octaves;
        public FloatProperty lateralScale, verticalScale, persistance, lacunarity;
    }
}