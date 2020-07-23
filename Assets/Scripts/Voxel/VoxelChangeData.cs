using UnityEngine;

namespace Voxel
{
    public struct VoxelChangeData
    {
        public byte? id, density, orientation;
        public bool? hasBlock, isBreakable, natural;
        public Color32? color;

        public override string ToString() => $"Texture: {id}, Is Block: {hasBlock}, Density: {density}, Orientation: {orientation}, Breakable: {isBreakable}";

        public void Merge(in VoxelChangeData newChange)
        {
            if (newChange.id.HasValue) id = newChange.id.Value;
            if (newChange.hasBlock.HasValue) hasBlock = newChange.hasBlock.Value;
            if (newChange.density.HasValue) density = newChange.density.Value;
            if (newChange.orientation.HasValue) orientation = newChange.orientation.Value;
            if (newChange.isBreakable.HasValue) isBreakable = newChange.isBreakable.Value;
            if (newChange.natural.HasValue) natural = newChange.natural.Value;
            if (newChange.color.HasValue) color = newChange.color.Value;
        }
    }
}