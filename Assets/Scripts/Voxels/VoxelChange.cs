using System.Collections.Generic;
using Swihoni.Util.Math;
using UnityEngine;

namespace Voxels
{
    public enum VoxelVolumeForm : byte
    {
        Single,
        Spherical,
        Cylindrical,
        Wall,
        Prism
    }

    public struct VoxelChange
    {
        public Position3Int? position;
        public byte? texture, density, orientation;
        public bool? hasBlock, isBreakable, natural, replace, modifiesBlocks, noRandom, revert;
        public Color32? color;
        public float? magnitude, yaw;
        public VoxelVolumeForm? form;
        public Position3Int? upperBound;
        public List<Voxel> undo;

        #region Generated

        public void Merge(in VoxelChange change)
        {
            if (change.position.HasValue) position = change.position.Value;
            if (change.texture.HasValue) texture = change.texture.Value;
            if (change.hasBlock.HasValue) hasBlock = change.hasBlock.Value;
            if (change.density.HasValue) density = change.density.Value;
            if (change.orientation.HasValue) orientation = change.orientation.Value;
            if (change.isBreakable.HasValue) isBreakable = change.isBreakable.Value;
            if (change.natural.HasValue) natural = change.natural.Value;
            if (change.replace.HasValue) replace = change.replace.Value;
            if (change.modifiesBlocks.HasValue) modifiesBlocks = change.modifiesBlocks.Value;
            if (change.revert.HasValue) revert = change.revert.Value;
            if (change.noRandom.HasValue) noRandom = change.noRandom.Value;
            if (change.color.HasValue) color = change.color.Value;
            if (change.magnitude.HasValue) magnitude = change.magnitude.Value;
            if (change.yaw.HasValue) yaw = change.yaw.Value;
            if (change.form.HasValue) form = change.form.Value;
            if (change.upperBound.HasValue) upperBound = change.upperBound.Value;
        }

        #endregion
    }
}