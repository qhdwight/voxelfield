using System.IO;
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

        public byte? texture, renderType, density, orientation;
        public bool? breakable, natural;

        public override string ToString() => $"Texture: {texture}, Render Type: {renderType}, Density: {density}, Orientation: {orientation}, Breakable: {breakable}";

        public static void Serialize(BinaryWriter message, VoxelChangeData changeData)
        {
            byte flags = 0;
            if (changeData.texture.HasValue) FlagUtil.SetFlag(ref flags, TextureFlagIndex);
            if (changeData.renderType.HasValue) FlagUtil.SetFlag(ref flags, RenderTypeFlagIndex);
            if (changeData.density.HasValue) FlagUtil.SetFlag(ref flags, DensityFlagIndex);
            if (changeData.orientation.HasValue) FlagUtil.SetFlag(ref flags, OrientationFlagIndex);
            if (changeData.breakable.HasValue) FlagUtil.SetFlag(ref flags, BreakableFlagIndex);
            if (changeData.natural.HasValue) FlagUtil.SetFlag(ref flags, NaturalFlagIndex);
            message.Write(flags);
            if (changeData.texture.HasValue) message.Write(changeData.texture.Value);
            if (changeData.renderType.HasValue) message.Write(changeData.renderType.Value);
            if (changeData.density.HasValue) message.Write(changeData.density.Value);
            if (changeData.orientation.HasValue) message.Write(changeData.orientation.Value);
            if (changeData.breakable.HasValue) message.Write(changeData.breakable.Value);
            if (changeData.natural.HasValue) message.Write(changeData.natural.Value);
        }

        public static VoxelChangeData Deserialize(BinaryReader message)
        {
            byte flags = message.ReadByte();
            var data = new VoxelChangeData();
            if (FlagUtil.HasFlag(flags, TextureFlagIndex)) data.texture = message.ReadByte();
            if (FlagUtil.HasFlag(flags, RenderTypeFlagIndex)) data.renderType = message.ReadByte();
            if (FlagUtil.HasFlag(flags, DensityFlagIndex)) data.density = message.ReadByte();
            if (FlagUtil.HasFlag(flags, OrientationFlagIndex)) data.orientation = message.ReadByte();
            if (FlagUtil.HasFlag(flags, BreakableFlagIndex)) data.breakable = message.ReadBoolean();
            if (FlagUtil.HasFlag(flags, NaturalFlagIndex)) data.natural = message.ReadBoolean();
            return data;
        }

        public void Merge(VoxelChangeData newChange)
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