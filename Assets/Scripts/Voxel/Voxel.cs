using System;
using UnityEngine;

namespace Voxel
{
    public static class VoxelId
    {
        public const byte Dirt = 1, Grass = 2, Stone = 3, Wood = 4, Last = Wood;

        public static string Name(byte id)
        {
            switch (id)
            {
                case Dirt:  return nameof(Dirt);
                case Grass: return nameof(Grass);
                case Stone: return nameof(Stone);
                case Wood:  return nameof(Wood);
                default:    throw new ArgumentOutOfRangeException(nameof(id), id, null);
            }
        }
    }
    
    public static class Orientation
    {
        public const byte None = 0, North = 1, East = 2, South = 3, West = 4, Up = 5, Down = 6;
    }
    
    [Flags]
    public enum VoxelFlags : ushort
    {
        None = 0,
        Block = 1,
        Breakable = 2,
        Natural = 4,
    }

    public struct Voxel
    {
        public const float
            TileSize = 256.0f,
            ImageSize = 1024.0f,
            TileRatio = TileSize / ImageSize,
            PixelRatio = 1.0f / ImageSize;

        public byte texture, density, orientation;
        public VoxelFlags flags;
        public Color32 color;
        
        public bool HasBlock
        {
            get => (flags & VoxelFlags.Block) == VoxelFlags.Block;
            set
            {
                if (value) flags |= VoxelFlags.Block;
                else flags &= ~VoxelFlags.Block;
            }
        }

        public bool OnlySmooth => !HasBlock;
        
        public bool IsBreakable
        {
            get => (flags & VoxelFlags.Breakable) == VoxelFlags.Breakable;
            set
            {
                if (value) flags |= VoxelFlags.Breakable;
                else flags &= ~VoxelFlags.Breakable;
            }
        }
        
        public bool IsNatural
        {
            get => (flags & VoxelFlags.Natural) == VoxelFlags.Natural;
            set
            {
                if (value) flags |= VoxelFlags.Natural;
                else flags &= ~VoxelFlags.Natural;
            }
        }

        public void SetVoxelData(in VoxelChangeData changeData)
        {
            if (changeData.id.HasValue) texture = changeData.id.Value;
            if (changeData.hasBlock.HasValue) HasBlock = changeData.hasBlock.Value;
            if (changeData.density.HasValue) density = changeData.density.Value;
            if (changeData.isBreakable.HasValue) IsBreakable = changeData.isBreakable.Value;
            if (changeData.orientation.HasValue) orientation = changeData.orientation.Value;
            if (changeData.natural.HasValue) IsNatural = changeData.natural.Value;
            if (changeData.color.HasValue) color = changeData.color.Value;
        }

        public Vector2Int TexturePosition()
        {
            Vector2Int tile;
            switch (texture)
            {
                default:
                case VoxelId.Stone:
                    tile = new Vector2Int(0, 0);
                    break;
                case VoxelId.Dirt:
                    tile = new Vector2Int(3, 0);
                    break;
                case VoxelId.Grass:
                    tile = new Vector2Int(1, 0);
                    break;
                case VoxelId.Wood:
                    tile = new Vector2Int(0, 1);
                    break;
            }
            return tile;
        }

        public int FaceUVs(Vector2[] uvs)
        {
            Vector2Int tilePos = TexturePosition();
            if (HasBlock)
            {
                float x = TileRatio * tilePos.x, y = TileRatio * tilePos.y;
                uvs[0] = new Vector2(x + TileRatio, y);
                uvs[1] = new Vector2(x + TileRatio, y + TileRatio);
                uvs[2] = new Vector2(x, y + TileRatio);
                uvs[3] = new Vector2(x, y);
                return 4;
            }
            else
            {
                float x = TileRatio * tilePos.x, y = TileRatio * tilePos.y;
                uvs[0] = new Vector2(x, y);
                uvs[2] = new Vector2(x + TileRatio, y);
                uvs[1] = new Vector2(x, y + TileRatio);
                return 3;
            }
        }

        public override string ToString() => $"Texture: {texture}, Density: {density}, Orientation: {orientation}, Flags: {flags}";

        public bool ShouldRenderBlock(byte direction) => OnlySmooth;
    }
}