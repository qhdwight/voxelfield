using System;
using System.Diagnostics.CodeAnalysis;
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
        public byte? texture, density, orientation;
        public bool? hasBlock, isBreakable, natural, replace, modifiesBlocks, noRandom;
        public Color32? color;
        public float? magnitude, yaw;
        public VoxelVolumeForm? form;
        public Position3Int? upperBound;
        
        public override string ToString() =>
            $"{nameof(texture)}: {texture}, {nameof(density)}: {density}, {nameof(orientation)}: {orientation}, {nameof(hasBlock)}: {hasBlock}, {nameof(isBreakable)}: {isBreakable}, {nameof(natural)}: {natural}, {nameof(replace)}: {replace}, {nameof(modifiesBlocks)}: {modifiesBlocks}, {nameof(noRandom)}: {noRandom}, {nameof(color)}: {color}, {nameof(magnitude)}: {magnitude}, {nameof(yaw)}: {yaw}, {nameof(form)}: {form}, {nameof(upperBound)}: {upperBound}";

        private bool Equals(in VoxelChange other) => texture == other.texture && density == other.density && orientation == other.orientation && hasBlock == other.hasBlock
                                                  && isBreakable == other.isBreakable && natural == other.natural && replace == other.replace
                                                  && modifiesBlocks == other.modifiesBlocks && noRandom == other.noRandom
                                                  && Nullable.Equals(color, other.color) && Nullable.Equals(magnitude, other.magnitude)
                                                  && Nullable.Equals(yaw, other.yaw) && form == other.form && Nullable.Equals(upperBound, other.upperBound);

        public override bool Equals(object other) => other is VoxelChange otherChange && Equals(otherChange);

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = texture.GetHashCode();
                hashCode = (hashCode * 397) ^ density.GetHashCode();
                hashCode = (hashCode * 397) ^ orientation.GetHashCode();
                hashCode = (hashCode * 397) ^ hasBlock.GetHashCode();
                hashCode = (hashCode * 397) ^ isBreakable.GetHashCode();
                hashCode = (hashCode * 397) ^ natural.GetHashCode();
                hashCode = (hashCode * 397) ^ replace.GetHashCode();
                hashCode = (hashCode * 397) ^ modifiesBlocks.GetHashCode();
                hashCode = (hashCode * 397) ^ noRandom.GetHashCode();
                hashCode = (hashCode * 397) ^ color.GetHashCode();
                hashCode = (hashCode * 397) ^ magnitude.GetHashCode();
                hashCode = (hashCode * 397) ^ yaw.GetHashCode();
                hashCode = (hashCode * 397) ^ form.GetHashCode();
                hashCode = (hashCode * 397) ^ upperBound.GetHashCode();
                return hashCode;
            }
        }

        public void Merge(in VoxelChange newChange)
        {
            if (newChange.texture.HasValue) texture = newChange.texture.Value;
            if (newChange.hasBlock.HasValue) hasBlock = newChange.hasBlock.Value;
            if (newChange.density.HasValue) density = newChange.density.Value;
            if (newChange.orientation.HasValue) orientation = newChange.orientation.Value;
            if (newChange.isBreakable.HasValue) isBreakable = newChange.isBreakable.Value;
            if (newChange.natural.HasValue) natural = newChange.natural.Value;
            if (newChange.replace.HasValue) replace = newChange.replace.Value;
            if (newChange.modifiesBlocks.HasValue) modifiesBlocks = newChange.modifiesBlocks.Value;
            if (newChange.noRandom.HasValue) noRandom = newChange.noRandom.Value;
            if (newChange.color.HasValue) color = newChange.color.Value;
            if (newChange.magnitude.HasValue) magnitude = newChange.magnitude.Value;
            if (newChange.yaw.HasValue) yaw = newChange.yaw.Value;
            if (newChange.form.HasValue) form = newChange.form.Value;
            if (newChange.upperBound.HasValue) upperBound = newChange.upperBound.Value;
        }
    }
}