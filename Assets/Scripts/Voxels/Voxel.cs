﻿using System;
using UnityEngine;

namespace Voxels
{
    public static class VoxelTexture
    {
        public const byte Solid = 0, Checkered = 1, Striped = 2, Speckled = 3, Last = Speckled;

        public static string Name(byte id) => id switch
        {
            Solid     => nameof(Solid),
            Checkered => nameof(Checkered),
            Striped   => nameof(Striped),
            Speckled  => nameof(Speckled),
            _         => throw new ArgumentOutOfRangeException(nameof(id), id, null)
        };
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
        Breathable = 8
    }

    public struct Voxel
    {
        public static readonly Color32 Dirt = new(39, 25, 10, 255),
                                       Stone = new(39, 39, 39, 255),
                                       // Grass = new Color32(68, 144, 71, 255),
                                       Grass = new(68, 144, 71, 255),
                                       Wood = new(132, 83, 40, 255);

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
            private set
            {
                if (value) flags |= VoxelFlags.Block;
                else flags &= ~VoxelFlags.Block;
            }
        }

        public bool OnlySmooth => !HasBlock;

        public bool IsBreakable
        {
            get => (flags & VoxelFlags.Breakable) == VoxelFlags.Breakable;
            private set
            {
                if (value) flags |= VoxelFlags.Breakable;
                else flags &= ~VoxelFlags.Breakable;
            }
        }

        public bool IsUnbreakable => !IsBreakable;

        public bool IsNatural
        {
            get => (flags & VoxelFlags.Natural) == VoxelFlags.Natural;
            private set
            {
                if (value) flags |= VoxelFlags.Natural;
                else flags &= ~VoxelFlags.Natural;
            }
        }

        public bool IsBreathable
        {
            get => (flags & VoxelFlags.Breathable) == VoxelFlags.Breathable;
            private set
            {
                if (value) flags |= VoxelFlags.Breathable;
                else flags &= ~VoxelFlags.Breathable;
            }
        }

        public void SetVoxelData(in VoxelChange change)
        {
            if (change.texture.HasValue) texture = change.texture.Value;
            if (change.hasBlock.HasValue) HasBlock = change.hasBlock.Value;
            if (change.density.HasValue) density = change.density.Value;
            if (change.isBreakable.HasValue) IsBreakable = change.isBreakable.Value;
            if (change.orientation.HasValue) orientation = change.orientation.Value;
            if (change.natural.HasValue) IsNatural = change.natural.Value;
            if (change.isBreathable.HasValue) IsBreathable = change.isBreathable.Value;
            if (change.color.HasValue) color = change.color.Value;
        }

        public Vector2Int TexturePosition() => texture switch
        {
            VoxelTexture.Checkered => new Vector2Int(0, 0),
            VoxelTexture.Solid     => new Vector2Int(1, 0),
            VoxelTexture.Striped   => new Vector2Int(0, 1),
            VoxelTexture.Speckled  => new Vector2Int(1, 1),
            _                      => throw new ArgumentOutOfRangeException(nameof(texture), texture, null)
        };

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