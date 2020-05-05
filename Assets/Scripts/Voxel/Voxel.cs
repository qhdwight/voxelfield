using UnityEngine;

namespace Voxel
{
    public static class VoxelTexture
    {
        public const byte NONE = 0, DIRT = 1, GRASS = 2, STONE = 3;
    }

    public static class VoxelRenderType
    {
        public const byte NONE = 0, BLOCK = 1, SMOOTH = 2;
    }

    public static class Orientation
    {
        public const byte NONE = 0, NORTH = 1, EAST = 2, SOUTH = 3, WEST = 4, UP = 5, DOWN = 6;
    }

    public struct Voxel
    {
        public const float
            TILE_SIZE = 34.0f,
            IMAGE_SIZE = 136.0f,
            TILE_RATIO = TILE_SIZE / IMAGE_SIZE,
            PIXEL_RATIO = 1.0f / IMAGE_SIZE;

        public byte texture, renderType, density, orientation;
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
                case VoxelTexture.STONE:
                    tile = new Vector2Int(0, 0);
                    break;
                case VoxelTexture.DIRT:
                    tile = new Vector2Int(3, 0);
                    break;
                case VoxelTexture.GRASS:
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
                case VoxelRenderType.BLOCK:
                {
                    float x = TILE_RATIO * tilePos.x, y = TILE_RATIO * tilePos.y;
                    uvs[0] = new Vector2(
                                         x + TILE_RATIO - PIXEL_RATIO,
                                         y + PIXEL_RATIO);
                    uvs[1] = new Vector2(
                                         x + TILE_RATIO - PIXEL_RATIO,
                                         y + TILE_RATIO - PIXEL_RATIO);
                    uvs[2] = new Vector2(
                                         x + PIXEL_RATIO,
                                         y + TILE_RATIO - PIXEL_RATIO);
                    uvs[3] = new Vector2(
                                         x + PIXEL_RATIO,
                                         y + PIXEL_RATIO);
                    return 4;
                }
                case VoxelRenderType.SMOOTH:
                {
                    float x = TILE_RATIO * tilePos.x, y = TILE_RATIO * tilePos.y;
                    uvs[0] = new Vector2(
                                         x + PIXEL_RATIO,
                                         y + PIXEL_RATIO
                                        );
                    uvs[2] = new Vector2(
                                         x + TILE_RATIO - PIXEL_RATIO,
                                         y + PIXEL_RATIO
                                        );
                    uvs[1] = new Vector2(
                                         x + PIXEL_RATIO,
                                         y + TILE_RATIO - PIXEL_RATIO
                                        );
                    return 3;
                }
            }
            return 0;
        }

        public override string ToString() { return $"Texture: {texture}, Render Type: {renderType}, Density: {density}, Breakable: {breakable}, Orientation: {orientation}"; }

        public bool ShouldRender(byte direction) { return renderType == VoxelRenderType.SMOOTH || renderType == VoxelRenderType.NONE; }
    }
}