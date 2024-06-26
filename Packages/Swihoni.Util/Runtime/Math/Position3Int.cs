using System;
using System.Diagnostics.CodeAnalysis;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Util.Math
{
    public struct Position3Int : IEquatable<Position3Int>
    {
        public int x, y, z;

//        public static readonly Position3Int zero = new Position3Int(0, 0, 0);

        public Position3Int(int x, int y, int z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public Position3Int(int i) : this(i, i, i) { }

        public bool Equals(Position3Int other) => this == other;

        public override bool Equals(object other) => other is Position3Int && GetHashCode() == other.GetHashCode();

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = x;
                hashCode = (hashCode * 397) ^ y;
                hashCode = (hashCode * 397) ^ z;
                return hashCode;
            }
        }

        public override string ToString() => $"[{x}, {y}, {z}]";

        public static void Serialize(Position3Int position, NetDataWriter writer)
        {
            writer.Put(position.x);
            writer.Put(position.y);
            writer.Put(position.z);
        }

        public static Position3Int Deserialize(NetDataReader reader) => new(reader.GetInt(),
                                                                            reader.GetInt(),
                                                                            reader.GetInt());

        // public bool InsideDimension(Dimension dimension) =>
        //     x >= dimension.lowerBound.x && y >= dimension.lowerBound.y && z >= dimension.lowerBound.z
        //  && x <= dimension.upperBound.x && y <= dimension.upperBound.y && z <= dimension.upperBound.z;

        public static float Distance(Position3Int p1, Position3Int p2) => Mathf.Sqrt(DistanceSquared(p1, p2));

        public static float DistanceSquared(Position3Int p1, Position3Int p2) => (p2.x - p1.x).Square() + (p2.y - p1.y).Square() + (p2.z - p1.z).Square();

        public static float DistanceFromOrigin(Position3Int p) => Mathf.Sqrt(p.x.Square() + p.y.Square() + p.z.Square());

        public static float DistanceFromOriginSquared(Position3Int p) => p.x.Square() + p.y.Square() + p.z.Square();

        public static bool operator ==(Position3Int p1, Position3Int p2) => p1.x == p2.x && p1.y == p2.y && p1.z == p2.z;

        public static bool operator !=(Position3Int p1, Position3Int p2) => p1.x != p2.x || p1.y != p2.y || p1.z != p2.z;

        public static Position3Int operator +(Position3Int p1, Position3Int p2) => new(p1.x + p2.x, p1.y + p2.y, p1.z + p2.z);

        public static Position3Int operator -(Position3Int p1, Position3Int p2) => new(p1.x - p2.x, p1.y - p2.y, p1.z - p2.z);

        public static Position3Int operator *(Position3Int p, int s) => new(p.x * s, p.y * s, p.z * s);

        public static Position3Int operator /(Position3Int p, int s) => new(p.x / s, p.y / s, p.z / s);

        public static Position3Int operator -(Position3Int p) => new(-p.x, -p.y, -p.z);

        public static implicit operator Vector3(Position3Int p) => new(p.x, p.y, p.z);

        public static explicit operator Position3Int(Vector3 v) => new(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y), Mathf.RoundToInt(v.z));
    }
}