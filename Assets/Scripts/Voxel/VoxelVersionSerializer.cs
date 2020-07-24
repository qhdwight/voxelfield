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
    [SuppressMessage("ReSharper", "InconsistentNaming"), SuppressMessage("ReSharper", "UnusedMember.Local")]
    public static class VoxelVersionSerializer
    {
        private delegate VoxelChange Deserializer(NetDataReader reader);

        private class DeserializerAttribute : Attribute
        {
            internal string Version { get; }

            public DeserializerAttribute(string version) => Version = version;
        }

        [Deserializer("0.0.11")]
        private static VoxelChange Deserialize_0_0_11(NetDataReader reader)
        {
            byte flags = reader.GetByte();
            VoxelChange change = default;
            if (FlagUtil.HasFlag(flags, 0)) change.id = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 1)) change.hasBlock = reader.GetByte() == 1;
            if (FlagUtil.HasFlag(flags, 2)) change.density = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 3)) change.orientation = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 4)) change.isBreakable = reader.GetBool();
            if (FlagUtil.HasFlag(flags, 5)) change.natural = reader.GetBool();
            if (FlagUtil.HasFlag(flags, 6)) change.color = reader.GetColor32();
            return change;
        }

        [Deserializer("0.0.12")]
        private static VoxelChange Deserialize_0_0_12(NetDataReader reader)
        {
            ushort flags = reader.GetUShort();
            VoxelChange change = default;
            if (FlagUtil.HasFlag(flags, 0)) change.id = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 1)) change.density = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 2)) change.orientation = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 3)) change.color = reader.GetColor32();
            if (FlagUtil.HasFlag(flags, 4)) change.hasBlock = FlagUtil.HasFlag(flags, 5);
            if (FlagUtil.HasFlag(flags, 6)) change.isBreakable = FlagUtil.HasFlag(flags, 7);
            if (FlagUtil.HasFlag(flags, 8)) change.natural = FlagUtil.HasFlag(flags, 9);
            return change;
        }

        private static readonly Dictionary<string, Deserializer> Deserializers = typeof(VoxelVersionSerializer)
                                                                                .GetMethods(BindingFlags.Static | BindingFlags.NonPublic)
                                                                                .Where(method => method.GetCustomAttribute<DeserializerAttribute>() != null)
                                                                                .ToDictionary(method => method.GetCustomAttribute<DeserializerAttribute>().Version,
                                                                                              method => (Deserializer) Delegate.CreateDelegate(typeof(Deserializer), null, method));

        public static VoxelChange Deserialize(NetDataReader reader, string version)
        {
            try
            {
                return version == null || version == Application.version ? DeserializeLatest(reader) : Deserializers[version](reader);
            }
            catch (KeyNotFoundException)
            {
                Debug.LogError($"No available way to convert save from version: {version}");
                throw;
            }
        }

        public static void Serialize(in VoxelChange change, NetDataWriter writer)
        {
            ushort flags = 0;
            int position = writer.Length;
            writer.Put(flags);

            if (change.id.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 0);
                writer.Put(change.id.Value);
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

            /* Flags */
            if (change.modifiesBlocks.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 6);
                if (change.modifiesBlocks.Value) FlagUtil.SetFlag(ref flags, 7);
            }
            if (change.replaceGrassWithDirt.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 8);
                if (change.replaceGrassWithDirt.Value) FlagUtil.SetFlag(ref flags, 9);
            }
            if (change.hasBlock.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 10);
                if (change.hasBlock.Value) FlagUtil.SetFlag(ref flags, 11);
            }
            if (change.isBreakable.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 12);
                if (change.isBreakable.Value) FlagUtil.SetFlag(ref flags, 13);
            }
            if (change.natural.HasValue)
            {
                FlagUtil.SetFlag(ref flags, 14);
                if (change.natural.Value) FlagUtil.SetFlag(ref flags, 15);
            }

            FastBitConverter.GetBytes(writer.Data, position, flags);
        }

        private static VoxelChange DeserializeLatest(NetDataReader reader)
        {
            ushort flags = reader.GetUShort();
            VoxelChange change = default;

            if (FlagUtil.HasFlag(flags, 0)) change.id = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 1)) change.density = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 2)) change.orientation = reader.GetByte();
            if (FlagUtil.HasFlag(flags, 3)) change.color = reader.GetColor32();
            if (FlagUtil.HasFlag(flags, 4)) change.magnitude = reader.GetFloat();
            if (FlagUtil.HasFlag(flags, 5)) change.yaw = reader.GetFloat();
            /* Flags */
            if (FlagUtil.HasFlag(flags, 6)) change.modifiesBlocks = FlagUtil.HasFlag(flags, 7);
            if (FlagUtil.HasFlag(flags, 8)) change.replaceGrassWithDirt = FlagUtil.HasFlag(flags, 9);
            if (FlagUtil.HasFlag(flags, 10)) change.hasBlock = FlagUtil.HasFlag(flags, 11);
            if (FlagUtil.HasFlag(flags, 12)) change.isBreakable = FlagUtil.HasFlag(flags, 13);
            if (FlagUtil.HasFlag(flags, 14)) change.natural = FlagUtil.HasFlag(flags, 15);

            return change;
        }
    }
}