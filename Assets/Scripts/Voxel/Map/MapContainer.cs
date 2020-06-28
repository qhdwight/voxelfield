using System;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Util.Math;

namespace Voxel.Map
{
    [Serializable]
    public class DimensionComponent : ComponentBase
    {
        public Position3Int lowerBound, upperBound;
    }
    
    public class MapContainer : Container
    {
        public StringProperty name;
        public IntProperty terrainHeight;
        public DimensionComponent dimension;
        public NoiseComponent noise;
        
        public Dictionary<Position3Int, BrushStroke> BrushStrokes { get; set; } = new Dictionary<Position3Int, BrushStroke>();
        public Dictionary<Position3Int, VoxelChangeData> ChangedVoxels { get; set; } = new Dictionary<Position3Int, VoxelChangeData>();
    }
}