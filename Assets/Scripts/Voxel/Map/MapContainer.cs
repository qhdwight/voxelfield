using Swihoni.Components;

namespace Voxel.Map
{
    public class MapContainer : Container
    {
        public StringProperty name;
        public IntProperty terrainHeight;
        public DimensionComponent dimension;
        public NoiseComponent noise;

        public ChangedVoxelsProperty changedVoxels;
        public ModelsProperty models;
    }
}