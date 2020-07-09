using LiteNetLib.Utils;
using Swihoni.Util;
using UnityEngine;

namespace Voxel
{
    public struct VoxelChangeData
    {
        public const int IdFlagIndex = 0,
                         RenderTypeFlagIndex = 1,
                         DensityFlagIndex = 2,
                         OrientationFlagIndex = 3,
                         BreakableFlagIndex = 4,
                         NaturalFlagIndex = 5,
                         ColorFlagIndex = 6;

        public byte? id, density, orientation;
        public VoxelRenderType? renderType;
        public bool? breakable, natural;
        public Color32? color;

        public override string ToString() => $"Texture: {id}, Render Type: {renderType}, Density: {density}, Orientation: {orientation}, Breakable: {breakable}";

        public static void Serialize(in VoxelChangeData changeData, NetDataWriter writer)
        {
            byte flags = 0;
            if (changeData.id.HasValue) FlagUtil.SetFlag(ref flags, IdFlagIndex);
            if (changeData.renderType.HasValue) FlagUtil.SetFlag(ref flags, RenderTypeFlagIndex);
            if (changeData.density.HasValue) FlagUtil.SetFlag(ref flags, DensityFlagIndex);
            if (changeData.orientation.HasValue) FlagUtil.SetFlag(ref flags, OrientationFlagIndex);
            if (changeData.breakable.HasValue) FlagUtil.SetFlag(ref flags, BreakableFlagIndex);
            if (changeData.natural.HasValue) FlagUtil.SetFlag(ref flags, NaturalFlagIndex);
            if (changeData.color.HasValue) FlagUtil.SetFlag(ref flags, ColorFlagIndex);
            writer.Put(flags);
            if (changeData.id.HasValue) writer.Put(changeData.id.Value);
            if (changeData.renderType.HasValue) writer.Put((byte) changeData.renderType.Value);
            if (changeData.density.HasValue) writer.Put(changeData.density.Value);
            if (changeData.orientation.HasValue) writer.Put(changeData.orientation.Value);
            if (changeData.breakable.HasValue) writer.Put(changeData.breakable.Value);
            if (changeData.natural.HasValue) writer.Put(changeData.natural.Value);
            if (changeData.color.HasValue) PutColor(writer, changeData.color.Value);
        }

        public static VoxelChangeData Deserialize(NetDataReader reader)
        {
            byte flags = reader.GetByte();
            var data = new VoxelChangeData();
            if (FlagUtil.HasFlag(flags, IdFlagIndex)) data.id = reader.GetByte();
            if (FlagUtil.HasFlag(flags, RenderTypeFlagIndex)) data.renderType = (VoxelRenderType) reader.GetByte();
            if (FlagUtil.HasFlag(flags, DensityFlagIndex)) data.density = reader.GetByte();
            if (FlagUtil.HasFlag(flags, OrientationFlagIndex)) data.orientation = reader.GetByte();
            if (FlagUtil.HasFlag(flags, BreakableFlagIndex)) data.breakable = reader.GetBool();
            if (FlagUtil.HasFlag(flags, NaturalFlagIndex)) data.natural = reader.GetBool();
            if (FlagUtil.HasFlag(flags, NaturalFlagIndex)) data.color = GetColor32(reader);
            return data;
        }

        public void Merge(in VoxelChangeData newChange)
        {
            if (newChange.id.HasValue) id = newChange.id.Value;
            if (newChange.renderType.HasValue) renderType = newChange.renderType.Value;
            if (newChange.density.HasValue) density = newChange.density.Value;
            if (newChange.orientation.HasValue) orientation = newChange.orientation.Value;
            if (newChange.breakable.HasValue) breakable = newChange.breakable.Value;
            if (newChange.natural.HasValue) natural = newChange.natural.Value;
            if (newChange.color.HasValue) color = newChange.color.Value;
        }

        private static void PutColor(NetDataWriter writer, in Color32 color)
        {
            writer.Put(color.r);
            writer.Put(color.g);
            writer.Put(color.b);
            writer.Put(color.a);
        }

        public static Color32 GetColor32(NetDataReader reader)
            => new Color32(reader.GetByte(), reader.GetByte(), reader.GetByte(), reader.GetByte());
    }
}