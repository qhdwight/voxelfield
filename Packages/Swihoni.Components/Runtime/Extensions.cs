using LiteNetLib;
using LiteNetLib.Utils;
using UnityEngine;

namespace Swihoni.Components
{
    public static class Extensions
    {
        public static Vector3 GetVector3(this NetDataReader reader) => new Vector3(reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

        public static void Put(this NetDataWriter writer, in Vector3 vector)
        {
            writer.Put(vector.x);
            writer.Put(vector.y);
            writer.Put(vector.z);
        }

        public static Quaternion GetQuaternion(this NetDataReader reader) => new Quaternion(reader.GetFloat(), reader.GetFloat(), reader.GetFloat(), reader.GetFloat());

        public static void Put(this NetDataWriter writer, in Quaternion quaternion)
        {
            writer.Put(quaternion.x);
            writer.Put(quaternion.y);
            writer.Put(quaternion.z);
            writer.Put(quaternion.w);
        }

        public static int GetPlayerId(this NetPeer peer) { return peer.Id + 1; }
    }
}