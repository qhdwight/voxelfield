using System.IO;

namespace Voxel
{
    public struct NoiseData
    {
        public int seed, octaves;
        public float lateralScale, verticalScale, persistance, lacunarity;

        public static void Serialize(NoiseData data, BinaryWriter message)
        {
            message.Write(data.seed);
            message.Write(data.octaves);
            message.Write(data.lateralScale);
            message.Write(data.verticalScale);
            message.Write(data.persistance);
            message.Write(data.lacunarity);
        }

        public static NoiseData Deserialize(BinaryReader message)
        {
            return new NoiseData
            {
                seed = message.ReadInt32(),
                octaves = message.ReadInt32(),
                lateralScale = message.ReadSingle(),
                verticalScale = message.ReadSingle(),
                persistance = message.ReadSingle(),
                lacunarity = message.ReadSingle()
            };
        }
    }
}