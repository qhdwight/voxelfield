using LiteNetLib.Utils;

namespace Swihoni.Util.Math
{
    public struct Dimension
    {
        public static Dimension empty = new Dimension();

        public Position3Int lowerBound, upperBound;

        public Dimension(Position3Int lowerBound, Position3Int upperBound)
        {
            this.lowerBound = lowerBound;
            this.upperBound = upperBound;
        }

        public static void Serialize(Dimension dimension, NetDataWriter writer)
        {
            Position3Int.Serialize(dimension.lowerBound, writer);
            Position3Int.Serialize(dimension.upperBound, writer);
        }

        public static Dimension Deserialize(NetDataReader reader) => new Dimension(Position3Int.Deserialize(reader),
                                                                                   Position3Int.Deserialize(reader));
    }
}