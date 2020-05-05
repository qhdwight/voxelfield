using System.IO;
using Swihoni.Util.Math;

namespace Voxel
{
    public struct BrushStroke
    {
        public Position3Int center;
        public byte radius;
        public byte texture, renderType;

        public static void Serialize(BrushStroke stroke, BinaryWriter message)
        {
            message.Write(stroke.radius);
            message.Write(stroke.texture);
            message.Write(stroke.renderType);
        }

        public static BrushStroke Deserialize(BinaryReader message)
        {
            return new BrushStroke
            {
                radius = message.ReadByte(),
                texture = message.ReadByte(),
                renderType = message.ReadByte()
            };
        }
    }
}