namespace Swihoni.Util
{
    public static class FlagUtil
    {
        public const byte EmptyByteFlags = byte.MinValue;
        public const ushort EmptyUshortFlags = ushort.MinValue;
        public const uint EmptyUintFlags = uint.MinValue;
        public const ulong EmptyUlongFlags = ulong.MinValue;

        public const byte FullByteFlags = byte.MaxValue;
        public const ushort FullUshortFlags = ushort.MaxValue;
        public const uint FullUintFlags = uint.MaxValue;
        public const ulong FullUlongFlags = ulong.MaxValue;

        public static void SetFlag(ref byte flags, int index) => flags |= (byte) (1u << index);

        public static void SetFlag(ref ushort flags, int index) => flags |= (ushort) (1u << index);

        public static void SetFlag(ref uint flags, int index) => flags |= 1u << index;

        public static void SetFlag(ref ulong flags, int index) => flags |= 1ul << index;

        public static void UnsetFlag(ref ulong flags, int index) => flags &= ~(1ul << index);

        public static bool HasFlag(uint flags, int index) => (flags & (1u << index)) != 0;

        public static bool HasFlag(ulong flags, int index) => (flags & (1ul << index)) != 0;

        public static void ToggleFlag(ref byte flags, int flagIndex) => flags ^= (byte) (1u << flagIndex);

        public static void ToggleFlag(ref ushort flags, int flagIndex) => flags ^= (ushort) (1u << flagIndex);
    }
}