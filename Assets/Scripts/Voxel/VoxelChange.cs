using UnityEngine;

namespace Voxel
{
    public enum VoxelVolumeForm : byte
    {
        Sperhical, Cylindrical, Wall
    }
    
    public struct VoxelChange
    {
        public byte? texture, density, orientation;
        public bool? hasBlock, isBreakable, natural, replaceGrassWithDirt, modifiesBlocks;
        public Color32? color;
        public float? magnitude, yaw;
        public VoxelVolumeForm? form;

        public override string ToString() => $"Texture: {texture}, Is Block: {hasBlock}, Density: {density}, Orientation: {orientation}, Breakable: {isBreakable}, Form: {form}, Magnitude: {magnitude}, Yaw: {yaw}";

        public void Merge(in VoxelChange newChange)
        {
            if (newChange.texture.HasValue) texture = newChange.texture.Value;
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
            if (newChange.form.HasValue) form = newChange.form.Value;
        }
    }
}