using LiteNetLib.Utils;
using Swihoni.Util;

namespace Voxel
{
    public struct VoxelChangeData
    {
        public const int TextureFlagIndex = 0,
                         RenderTypeFlagIndex = 1,
                         DensityFlagIndex = 2,
                         OrientationFlagIndex = 3,
                         BreakableFlagIndex = 4,
                         NaturalFlagIndex = 5;

        public byte? texture, density, orientation;
        public VoxelRenderType? renderType;
        public bool? breakable, natural;

        public override string ToString() => $"Texture: {texture}, Render Type: {renderType}, Density: {density}, Orientation: {orientation}, Breakable: {breakable}";

        public static void Serialize(in VoxelChangeData changeData, NetDataWriter writer)
        {
            byte flags = 0;
            if (changeData.texture.HasValue) FlagUtil.SetFlag(ref flags, TextureFlagIndex);
            if (changeData.renderType.HasValue) FlagUtil.SetFlag(ref flags, RenderTypeFlagIndex);
            if (changeData.density.HasValue) FlagUtil.SetFlag(ref flags, DensityFlagIndex);
            if (changeData.orientation.HasValue) FlagUtil.SetFlag(ref flags, OrientationFlagIndex);
            if (changeData.breakable.HasValue) FlagUtil.SetFlag(ref flags, BreakableFlagIndex);
            if (changeData.natural.HasValue) FlagUtil.SetFlag(ref flags, NaturalFlagIndex);
            writer.Put(flags);
            if (changeData.texture.HasValue) writer.Put(changeData.texture.Value);
            if (changeData.renderType.HasValue) writer.Put((byte) changeData.renderType.Value);
            if (changeData.density.HasValue) writer.Put(changeData.density.Value);
            if (changeData.orientation.HasValue) writer.Put(changeData.orientation.Value);
            if (changeData.breakable.HasValue) writer.Put(changeData.breakable.Value);
            if (changeData.natural.HasValue) writer.Put(changeData.natural.Value);
        }

        public static VoxelChangeData Deserialize(NetDataReader reader)
        {
            byte flags = reader.GetByte();
            var data = new VoxelChangeData();
            if (FlagUtil.HasFlag(flags, TextureFlagIndex)) data.texture = reader.GetByte();
            if (FlagUtil.HasFlag(flags, RenderTypeFlagIndex)) data.renderType = (VoxelRenderType) reader.GetByte();
            if (FlagUtil.HasFlag(flags, DensityFlagIndex)) data.density = reader.GetByte();
            if (FlagUtil.HasFlag(flags, OrientationFlagIndex)) data.orientation = reader.GetByte();
            if (FlagUtil.HasFlag(flags, BreakableFlagIndex)) data.breakable = reader.GetBool();
            if (FlagUtil.HasFlag(flags, NaturalFlagIndex)) data.natural = reader.GetBool();
            return data;
        }

        public void Merge(in VoxelChangeData newChange)
        {
            if (newChange.texture.HasValue) texture = newChange.texture.Value;
            if (newChange.renderType.HasValue) renderType = newChange.renderType.Value;
            if (newChange.density.HasValue) density = newChange.density.Value;
            if (newChange.orientation.HasValue) orientation = newChange.orientation.Value;
            if (newChange.breakable.HasValue) breakable = newChange.breakable.Value;
            if (newChange.natural.HasValue) breakable = newChange.natural.Value;
        }
    }
}