using LiteNetLib.Utils;

namespace Voxel
{
    public struct NoiseData
    {
        public int seed, octaves;
        public float lateralScale, verticalScale, persistance, lacunarity;

        public static void Serialize(NoiseData data, NetDataWriter writer)
        {
            writer.Put(data.seed);
            writer.Put(data.octaves);
            writer.Put(data.lateralScale);
            writer.Put(data.verticalScale);
            writer.Put(data.persistance);
            writer.Put(data.lacunarity);
        }

        public static NoiseData Deserialize(NetDataReader message)
        {
            return new NoiseData
            {
                seed = message.GetInt(),
                octaves = message.GetInt(),
                lateralScale = message.GetFloat(),
                verticalScale = message.GetFloat(),
                persistance = message.GetFloat(),
                lacunarity = message.GetFloat()
            };
        }
    }
}