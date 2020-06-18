using LiteNetLib;
using LiteNetLib.Utils;
using Swihoni.Sessions;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel;
using Voxel.Map;

namespace Compound.Session
{
    public class VoxelInjector : SessionInjectorBase
    {
        protected internal virtual void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true)
            => ChunkManager.Singleton.SetVoxelData(worldPosition, change, chunk, updateMesh);

        protected internal virtual void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, ChangedVoxelsProperty changedVoxels = null)
            => ChunkManager.Singleton.RemoveVoxelRadius(worldPosition, radius, replaceGrassWithDirt, changedVoxels);

        protected override void OnHandleNewConnection(ConnectionRequest request)
        {
            if (request.Data.TryGetString(out string result) && result == Version.String)
                request.Accept();
            else
            {
                var writer = new NetDataWriter();
                writer.Put("Your version does not match that of the server.");
                request.Reject(writer);
            }
        }

        protected override void Stop() => MapManager.Singleton.SetMap("Menu");

        protected override Vector3 GetSpawnPosition()
        {
            int chunkSize = ChunkManager.Singleton.ChunkSize;
            Dimension dimension = MapManager.Singleton.Map.Dimension;
            var position = new Vector3
            {
                x = Random.Range(dimension.lowerBound.x * chunkSize, dimension.upperBound.x * chunkSize),
                y = 1000.0f,
                z = Random.Range(dimension.lowerBound.x * chunkSize, dimension.upperBound.x * chunkSize)
            };
            for (var _ = 0; _ < 32; _++)
            {
                if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, float.PositiveInfinity))
                    return hit.point + new Vector3 {y = 0.1f};
            }
            return new Vector3 {y = 8.0f};
        }
    }
}