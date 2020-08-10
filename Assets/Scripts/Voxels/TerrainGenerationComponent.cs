using System;
using Swihoni.Components;

namespace Voxels
{
    [Serializable]
    public class TerrainGenerationComponent : ComponentBase
    {
        public IntProperty seed;
        public ByteProperty octaves;
        public FloatProperty lateralScale, verticalScale, persistence, lacunarity;
        public VoxelChangeProperty grassVoxel, stoneVoxel;
        public IntProperty lowerBreakableHeight, upperBreakableHeight;
    }
}