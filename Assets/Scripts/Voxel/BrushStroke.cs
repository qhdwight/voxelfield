using LiteNetLib.Utils;
using Swihoni.Util.Math;

namespace Voxel
{
    public struct BrushStroke
    {
        public Position3Int center;
        public byte radius;
        public byte texture, renderType;

        public static void Serialize(BrushStroke stroke, NetDataWriter writer)
        {
            writer.Put(stroke.radius);
            writer.Put(stroke.texture);
            writer.Put(stroke.renderType);
        }

        public static BrushStroke Deserialize(NetDataReader reader)
        {
            return new BrushStroke
            {
                radius = reader.GetByte(),
                texture = reader.GetByte(),
                renderType = reader.GetByte()
            };
        }
    }
}