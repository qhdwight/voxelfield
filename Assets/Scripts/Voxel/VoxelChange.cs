using UnityEngine;

namespace Voxel
{
    public struct VoxelChange
    {
        public byte? id, density, orientation;
        public bool? hasBlock, isBreakable, natural, replaceGrassWithDirt, modifiesBlocks;
        public Color32? color;
        public float? magnitude, yaw;

        public override string ToString() => $"Texture: {id}, Is Block: {hasBlock}, Density: {density}, Orientation: {orientation}, Breakable: {isBreakable}, Magnitude: {magnitude}, Yaw: {yaw}";

        public void Merge(in VoxelChange newChange)
        {
            if (newChange.id.HasValue) id = newChange.id.Value;
            if (newChange.hasBlock.HasValue) hasBlock = newChange.hasBlock.Value;
            if (newChange.density.HasValue) density = newChange.density.Value;
            if (newChange.orientation.HasValue) orientation = newChange.orientation.Value;
            if (newChange.isBreakable.HasValue) isBreakable = newChange.isBreakable.Value;
            if (newChange.natural.HasValue) natural = newChange.natural.Value;
            if (newChange.replaceGrassWithDirt.HasValue) replaceGrassWithDirt = newChange.replaceGrassWithDirt.Value;
            if (newChange.modifiesBlocks.HasValue) modifiesBlocks = newChange.modifiesBlocks.Value;
            if (newChange.color.HasValue) color = newChange.color.Value;
            if (newChange.magnitude.HasValue) magnitude = newChange.magnitude.Value;
            if (newChange.yaw.HasValue) yaw = newChange.yaw.Value;
        }
    }
}