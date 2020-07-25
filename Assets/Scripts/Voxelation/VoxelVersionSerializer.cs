using System.Diagnostics.CodeAnalysis;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util;

namespace Voxelation
{
    [SuppressMessage("ReSharper", "InconsistentNaming"), SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class VoxelVersionSerializer
    {
        // private delegate VoxelChange Deserializer(NetDataReader reader);
        //
        // private class DeserializerForVersionAttribute : Attribute
        // {
        //     internal string[] Versions { get; }
        //
        //     public DeserializerForVersionAttribute(params string[] versions) => Versions = versions;
        // }
        //
        // [DeserializerForVersion("0.0.11")]
        // private static VoxelChange _Deserialize(NetDataReader reader)
        // {
        //     byte flags = reader.GetByte();
        //     VoxelChange change = default;
        //     if (FlagUtil.HasFlag(flags, 0)) change.id = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 1)) change.hasBlock = reader.GetByte() == 1;
        //     if (FlagUtil.HasFlag(flags, 2)) change.density = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 3)) change.orientation = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 4)) change.isBreakable = reader.GetBool();
        //     if (FlagUtil.HasFlag(flags, 5)) change.natural = reader.GetBool();
        //     if (FlagUtil.HasFlag(flags, 6)) change.color = reader.GetColor32();
        //     return change;
        // }
        //
        // [DeserializerForVersion("0.0.12", "1.0.0.0")]
        // private static VoxelChange __Deserialize(NetDataReader reader)
        // {
        //     ushort flags = reader.GetUShort();
        //     VoxelChange change = default;
        //     if (FlagUtil.HasFlag(flags, 0)) change.id = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 1)) change.density = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 2)) change.orientation = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 3)) change.color = reader.GetColor32();
        //     if (FlagUtil.HasFlag(flags, 4)) change.hasBlock = FlagUtil.HasFlag(flags, 5);
        //     if (FlagUtil.HasFlag(flags, 6)) change.isBreakable = FlagUtil.HasFlag(flags, 7);
        //     if (FlagUtil.HasFlag(flags, 8)) change.natural = FlagUtil.HasFlag(flags, 9);
        //     return change;
        // }
        //
        // [DeserializerForVersion("1.0.0.1")]
        // private static VoxelChange ___Deserialize(NetDataReader reader)
        // {
        //     ushort flags = reader.GetUShort();
        //     VoxelChange change = default;
        //     if (FlagUtil.HasFlag(flags, 0)) change.id = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 1)) change.density = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 2)) change.orientation = reader.GetByte();
        //     if (FlagUtil.HasFlag(flags, 3)) change.color = reader.GetColor32();
        //     if (FlagUtil.HasFlag(flags, 4)) change.magnitude = reader.GetFloat();
        //     if (FlagUtil.HasFlag(flags, 5)) change.yaw = reader.GetFloat();
        //     if (FlagUtil.HasFlag(flags, 6)) change.modifiesBlocks = FlagUtil.HasFlag(flags, 7);
        //     if (FlagUtil.HasFlag(flags, 8)) change.replaceGrassWithDirt = FlagUtil.HasFlag(flags, 9);
        //     if (FlagUtil.HasFlag(flags, 10)) change.hasBlock = FlagUtil.HasFlag(flags, 11);
        //     if (FlagUtil.HasFlag(flags, 12)) change.isBreakable = FlagUtil.HasFlag(flags, 13);
        //     if (FlagUtil.HasFlag(flags, 14)) change.natural = FlagUtil.HasFlag(flags, 15);
        //     return change;
        // }
        //
        // private static readonly Dictionary<string, Deserializer> Deserializers
        //     = typeof(VoxelVersionSerializer).GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
        //                                     .Where(method => method.GetCustomAttribute<DeserializerForVersionAttribute>() != null)
        //                                     .SelectMany(method => method.GetCustomAttribute<DeserializerForVersionAttribute>().Versions
        //                                                                 .Select(version => new {Method = method, Version = version}))
        //                                     .ToDictionary(pair => pair.Version,
        //                                                   pair => (Deserializer) Delegate.CreateDelegate(typeof(Deserializer), null, pair.Method));
        //
        // public static VoxelChange Deserialize(NetDataReader reader, string version = null)
        // {
        //     try
        //     {
        //         return version == null || version == Application.version ? DeserializeLatest(reader) : Deserializers[version](reader);
        //     }
        //     catch (KeyNotFoundException)
        //     {
        //         Debug.LogError($"No available way to convert save from version: {version}");
        //         throw;
        //     }
        // }

        public static VoxelChange Deserialize(NetDataReader reader, string version = null) => DeserializeLatest(reader);

        public static void Serialize(in VoxelChange change, NetDataWriter writer)
        {
            var flags = 0u;
            int position = writer.Length;
            writer.Put(flags);

            if (change.texture.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 0);
                writer.Put(change.texture.Value);
            }
            if (change.density.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 1);
                writer.Put(change.density.Value);
            }
            if (change.orientation.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 2);
                writer.Put(change.orientation.Value);
            }
            if (change.color.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 3);
                writer.PutColor32(change.color.Value);
            }
            if (change.magnitude.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 4);
                writer.Put(change.magnitude.Value);
            }
            if (change.yaw.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 5);
                writer.Put(change.yaw.Value);
            }
            if (change.form.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 6);
                writer.Put((byte) change.form.Value);
            }

            /* Flags */
            if (change.modifiesBlocks.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 22);
                if (change.modifiesBlocks.Value) FlagUtil.SetFlag(ref flags, 23);
            }
            if (change.replace.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 24);
                if (change.replace.Value) FlagUtil.SetFlag(ref flags, 25);
            }
            if (change.hasBlock.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 26);
                if (change.hasBlock.Value) FlagUtil.SetFlag(ref flags, 27);
            }
            if (change.isBreakable.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 28);
                if (change.isBreakable.Value) FlagUtil.SetFlag(ref flags, 29);
            }
            if (change.natural.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 30);
                if (change.natural.Value) FlagUtil.SetFlag(ref flags, 31);
            }

            FastBitConverter.GetBytes(writer.Data, position, flags);
        }

        private static VoxelChange DeserializeLatest(NetDataReader reader)
        {
            uint flags = reader.GetUInt();
            VoxelChange change = default;

            if (FlagUtil.HasFlag(flags, 0)) change.texture = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 1)) change.density = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 2)) change.orientation = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 3)) change.color = reader.GetColor32();
            // switch (change.texture)
            // {
            //     case 1:
            //         change.color = Voxel.Dirt;
            //         change.texture = VoxelTexture.Checkered;
            //         break;
            //     case 3:
            //         change.color = Voxel.Stone;
            //         change.texture = VoxelTexture.Checkered;
            //         break;
            //     case 2:
            //         change.color = Voxel.Grass;
            //         change.texture = VoxelTexture.Solid;
            //         break;
            //     case 4:
            //         change.color = Voxel.Wood;
            //         change.texture = VoxelTexture.Striped;
            //         break;
            // }
            // if (FlagUtil.HasFlag(flags, 4)) change.magnitude = reader.GetFloat();
            // if (FlagUtil.HasFlag(flags, 5)) change.yaw = reader.GetFloat();
            // if (FlagUtil.HasFlag(flags, 6)) change.form = (VoxelVolumeForm) reader.GetByte();
            // /* Flags */
            // if (FlagUtil.HasFlag(flags, 10)) change.hasBlock = FlagUtil.HasFlag(flags, 11);
            // if (FlagUtil.HasFlag(flags, 12)) change.isBreakable = FlagUtil.HasFlag(flags, 13);
            // if (FlagUtil.HasFlag(flags, 14)) change.natural = FlagUtil.HasFlag(flags, 15);
            if (FlagUtil.HasFlag(flags, 22)) change.modifiesBlocks = FlagUtil.HasFlag(flags, 23);
            if (FlagUtil.HasFlag(flags, 24)) change.replace = FlagUtil.HasFlag(flags, 25);
            if (FlagUtil.HasFlag(flags, 26)) change.hasBlock = FlagUtil.HasFlag(flags, 27);
            if (FlagUtil.HasFlag(flags, 28)) change.isBreakable = FlagUtil.HasFlag(flags, 29);
            if (FlagUtil.HasFlag(flags, 30)) change.natural = FlagUtil.HasFlag(flags, 31);

            return change;
        }
    }
}