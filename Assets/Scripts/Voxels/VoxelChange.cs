using System;
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
        public bool? hasBlock, isBreakable, natural, replace, modifiesBlocks, noRandom;
        public Color32? color;
        public float? magnitude, yaw;
        public VoxelVolumeForm? form;
        public Position3Int? upperBound;

        public override string ToString() =>
            $"{nameof(position)}: {position}, {nameof(texture)}: {texture}, {nameof(density)}: {density}, {nameof(orientation)}: {orientation}, {nameof(hasBlock)}: {hasBlock}, {nameof(isBreakable)}: {isBreakable}, {nameof(natural)}: {natural}, {nameof(replace)}: {replace}, {nameof(modifiesBlocks)}: {modifiesBlocks}, {nameof(noRandom)}: {noRandom}, {nameof(color)}: {color}, {nameof(magnitude)}: {magnitude}, {nameof(yaw)}: {yaw}, {nameof(form)}: {form}, {nameof(upperBound)}: {upperBound}";

        public bool Equals(VoxelChange other) => position.Equals(other.position) && texture == other.texture && density == other.density && orientation == other.orientation &&
                                                 hasBlock == other.hasBlock && isBreakable == other.isBreakable && natural == other.natural && replace == other.replace &&
                                                 modifiesBlocks == other.modifiesBlocks && noRandom == other.noRandom && Nullable.Equals(color, other.color) &&
                                                 Nullable.Equals(magnitude, other.magnitude) && Nullable.Equals(yaw, other.yaw) && form == other.form &&
                                                 Nullable.Equals(upperBound, other.upperBound);

        public override bool Equals(object other) => other is VoxelChange otherChange && Equals(otherChange);

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = position.GetHashCode();
                hashCode = (hashCode * 397) ^ texture.GetHashCode();
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

        public static bool operator ==(VoxelChange left, VoxelChange right) => left.Equals(right);
        public static bool operator !=(VoxelChange left, VoxelChange right) => !left.Equals(right);

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
            if (change.noRandom.HasValue) noRandom = change.noRandom.Value;
            if (change.color.HasValue) color = change.color.Value;
            if (change.magnitude.HasValue) magnitude = change.magnitude.Value;
            if (change.yaw.HasValue) yaw = change.yaw.Value;
            if (change.form.HasValue) form = change.form.Value;
            if (change.upperBound.HasValue) upperBound = change.upperBound.Value;
        }
    }
}