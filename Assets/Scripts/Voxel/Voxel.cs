using UnityEngine;

namespace Voxel
{
    public static class VoxelTexture
    {
        public const byte None = 0, Dirt = 1, Grass = 2, Stone = 3;
    }

    public enum VoxelRenderType : byte
    {
        None,
        Block,
        Smooth
    }

    public static class Orientation
    {
        public const byte None = 0, North = 1, East = 2, South = 3, West = 4, Up = 5, Down = 6;
    }

    public struct Voxel
    {
        public const float
            TileSize = 34.0f,
            ImageSize = 136.0f,
            TileRatio = TileSize / ImageSize,
            PixelRatio = 1.0f / ImageSize;

        public byte texture, density, orientation;
        public VoxelRenderType renderType;
        public bool breakable, natural;

        public void SetVoxelData(VoxelChangeData changeData)
        {
            if (changeData.texture.HasValue) texture = changeData.texture.Value;
            if (changeData.renderType.HasValue) renderType = changeData.renderType.Value;
            if (changeData.density.HasValue) density = changeData.density.Value;
            if (changeData.breakable.HasValue) breakable = changeData.breakable.Value;
            if (changeData.orientation.HasValue) orientation = changeData.orientation.Value;
            if (changeData.natural.HasValue) natural = changeData.natural.Value;
        }

        public Vector2Int TexturePosition()
        {
            Vector2Int tile;
            switch (texture)
            {
                default:
                case VoxelTexture.Stone:
                    tile = new Vector2Int(0, 0);
                    break;
                case VoxelTexture.Dirt:
                    tile = new Vector2Int(3, 0);
                    break;
                case VoxelTexture.Grass:
                    tile = new Vector2Int(1, 0);
                    break;
            }
            return tile;
        }

        public int FaceUVs(Vector2[] uvs)
        {
            Vector2Int tilePos = TexturePosition();
            switch (renderType)
            {
                case VoxelRenderType.Block:
                {
                    float x = TileRatio * tilePos.x, y = TileRatio * tilePos.y;
                    uvs[0] = new Vector2(x + TileRatio - PixelRatio,
                                         y + PixelRatio);
                    uvs[1] = new Vector2(x + TileRatio - PixelRatio,
                                         y + TileRatio - PixelRatio);
                    uvs[2] = new Vector2(x + PixelRatio,
                                         y + TileRatio - PixelRatio);
                    uvs[3] = new Vector2(x + PixelRatio,
                                         y + PixelRatio);
                    return 4;
                }
                case VoxelRenderType.Smooth:
                {
                    float x = TileRatio * tilePos.x, y = TileRatio * tilePos.y;
                    uvs[0] = new Vector2(x + PixelRatio,
                                         y + PixelRatio);
                    uvs[2] = new Vector2(x + TileRatio - PixelRatio,
                                         y + PixelRatio);
                    uvs[1] = new Vector2(x + PixelRatio,
                                         y + TileRatio - PixelRatio);
                    return 3;
                }
            }
            return 0;
        }

        public override string ToString() => $"Texture: {texture}, Render Type: {renderType}, Density: {density}, Breakable: {breakable}, Orientation: {orientation}";

        public bool ShouldRender(byte direction) => renderType == VoxelRenderType.Smooth || renderType == VoxelRenderType.None;
    }
}