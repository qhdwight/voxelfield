using System.IO;
using UnityEngine;

namespace Swihoni.Components
{
    public static class StreamExtensions
    {
        public static Vector3 ReadVector3(this BinaryReader reader) => new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        public static void Write(this BinaryWriter writer, in Vector3 vector)
        {
            writer.Write(vector.x);
            writer.Write(vector.y);
            writer.Write(vector.z);
        }

        public static Quaternion ReadQuaternion(this BinaryReader reader) => new Quaternion(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());

        public static void Write(this BinaryWriter writer, in Quaternion quaternion)
        {
            writer.Write(quaternion.x);
            writer.Write(quaternion.y);
            writer.Write(quaternion.z);
            writer.Write(quaternion.w);
        }
    }
}