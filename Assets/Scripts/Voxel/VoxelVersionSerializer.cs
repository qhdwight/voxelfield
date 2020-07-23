using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using LiteNetLib.Utils;
using Swihoni.Components;
using Swihoni.Util;
using UnityEngine;

namespace Voxel
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    public static class VoxelVersionSerializer
    {
        private delegate VoxelChangeData Deserializer(NetDataReader reader);

        private class DeserializerAttribute : Attribute
        {
            internal string Version { get; }

            public DeserializerAttribute(string version) => Version = version;
        }

        [Deserializer("0.0.11")]
        private static VoxelChangeData Deserialize_0_0_11(NetDataReader reader)
        {
            byte flags = reader.GetByte();
            VoxelChangeData data = default;
            if (FlagUtil.HasFlag(flags, 0)) data.id = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 1)) data.hasBlock = reader.GetByte() == 1;
            if (FlagUtil.HasFlag(flags, 2)) data.density = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 3)) data.orientation = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 4)) data.isBreakable = reader.GetBool();
            if (FlagUtil.HasFlag(flags, 5)) data.natural = reader.GetBool();
            if (FlagUtil.HasFlag(flags, 6)) data.color = reader.GetColor32();
            return data;
        }

        private static readonly Dictionary<string, Deserializer> Deserializers = typeof(VoxelVersionSerializer)
                                                                                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                                                                                .Where(method => method.GetCustomAttribute<DeserializerAttribute>() != null)
                                                                                .ToDictionary(method => method.GetCustomAttribute<DeserializerAttribute>().Version,
                                                                                              method => (Deserializer) Delegate.CreateDelegate(typeof(Deserializer), null, method));

        public static VoxelChangeData Deserialize(NetDataReader reader, string version)
            => version == null || version == Application.version ? Deserialize(reader) : Deserializers[version](reader);

        public static void Serialize(in VoxelChangeData changeData, NetDataWriter writer)
        {
            ushort flags = 0;
            int position = writer.Length;
            writer.Put(flags);

            if (changeData.id.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 0);
                writer.Put(changeData.id.Value);
            }
            if (changeData.density.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 1);
                writer.Put(changeData.density.Value);
            }
            if (changeData.orientation.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 2);
                writer.Put(changeData.orientation.Value);
            }
            if (changeData.color.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 3);
                writer.PutColor32(changeData.color.Value);
            }
            if (changeData.hasBlock.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 4);
                if (changeData.hasBlock.Value) FlagUtil.SetFlag(ref flags, 5);
            }
            if (changeData.isBreakable.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 6);
                if (changeData.isBreakable.Value) FlagUtil.SetFlag(ref flags, 7);
            }
            if (changeData.natural.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 8);
                if (changeData.natural.Value) FlagUtil.SetFlag(ref flags, 9);
            }

            FastBitConverter.GetBytes(writer.Data, position, flags);
        }

        private static VoxelChangeData Deserialize(NetDataReader reader)
        {
            ushort flags = reader.GetUShort();
            VoxelChangeData data = default;

            if (FlagUtil.HasFlag(flags, 0)) data.id = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 1)) data.density = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 2)) data.orientation = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 3)) data.color = reader.GetColor32();

            if (FlagUtil.HasFlag(flags, 4)) data.hasBlock = FlagUtil.HasFlag(flags, 5);
            if (FlagUtil.HasFlag(flags, 6)) data.isBreakable = FlagUtil.HasFlag(flags, 7);
            if (FlagUtil.HasFlag(flags, 8)) data.natural = FlagUtil.HasFlag(flags, 9);

            return data;
        }
    }
}