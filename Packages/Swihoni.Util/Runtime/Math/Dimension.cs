using System.IO;

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

        public static void Serialize(Dimension dimension, BinaryWriter message)
        {
            Position3Int.Serialize(dimension.lowerBound, message);
            Position3Int.Serialize(dimension.upperBound, message);
        }

        public static Dimension Deserialize(BinaryReader message) => new Dimension(Position3Int.Deserialize(message),
                                                                                   Position3Int.Deserialize(message));
    }
}