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
        public readonly struct Key
        {
            public readonly Position3Int origin;
            public readonly VoxelVolumeForm form;
            // Spherical, Cylindrical, Wall
            public readonly float magnitude;
            // Prism
            public readonly Position3Int upperBound;

            public bool Equals(in Key other) => origin.Equals(other.origin) && form == other.form && magnitude.Equals(other.magnitude) && upperBound.Equals(other.upperBound);

            public override bool Equals(object other) => other is Key otherKey && Equals(otherKey);

            public override int GetHashCode()
            {
                unchecked
                {
                    int hashCode = origin.GetHashCode();
                    hashCode = (hashCode * 397) ^ (int) form;
                    hashCode = (hashCode * 397) ^ magnitude.GetHashCode();
                    hashCode = (hashCode * 397) ^ upperBound.GetHashCode();
                    return hashCode;
                }
            }

            public static bool operator ==(in Key left, in Key right) => left.Equals(right);
            public static bool operator !=(in Key left, in Key right) => !(left == right);
        }
        
        public byte? texture, density, orientation;
        public bool? hasBlock, isBreakable, natural, replace, modifiesBlocks, noRandom;
        public Color32? color;

        public override string ToString() =>
            $"{nameof(texture)}: {texture}, {nameof(density)}: {density}, {nameof(orientation)}: {orientation}, {nameof(hasBlock)}: {hasBlock}, {nameof(isBreakable)}: {isBreakable}, {nameof(natural)}: {natural}, {nameof(replace)}: {replace}, {nameof(modifiesBlocks)}: {modifiesBlocks}, {nameof(noRandom)}: {noRandom}, {nameof(color)}: {color}";
        

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
        }
    }
}