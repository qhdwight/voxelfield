using LiteNetLib;
using LiteNetLib.Utils;
using Swihoni.Components;
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

        protected internal virtual void VoxelTransaction(VoxelChangeTransaction uncommitted) => uncommitted.Commit();

        protected internal virtual void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, ChangedVoxelsProperty changedVoxels = null)
            => ChunkManager.Singleton.RemoveVoxelRadius(worldPosition, radius, replaceGrassWithDirt, changedVoxels);

        private readonly NetDataWriter m_RejectionWriter = new NetDataWriter();

        protected override void OnHandleNewConnection(ConnectionRequest request)
        {
            void Reject(string message)
            {
                m_RejectionWriter.Reset();
                m_RejectionWriter.Put(message);
                request.Reject(m_RejectionWriter);
            }
            int nextPeerId = ((NetworkedSessionBase) Manager).Socket.NetworkManager.PeekNextId();
            if (nextPeerId >= SessionBase.MaxPlayers - 1) Reject("Too many players are already connected to the server!");
            else if (request.Data.TryGetString(out string result) && result != Version.String) Reject("Your version does not match that of the server.");
            else request.Accept();
        }

        protected override void Stop() => MapManager.Singleton.UnloadMap();

        protected override Vector3 GetSpawnPosition()
        {
            int chunkSize = ChunkManager.Singleton.ChunkSize;
            DimensionComponent dimension = MapManager.Singleton.Map.dimension;
            Position3Int lower = dimension.lowerBound, upper = dimension.upperBound;
            var position = new Vector3
            {
                x = Random.Range(lower.x * chunkSize, (upper.x + 1) * chunkSize),
                y = 1000.0f,
                z = Random.Range(lower.z * chunkSize, (upper.z + 1) * chunkSize)
            };
            for (var _ = 0; _ < 32; _++)
            {
                if (Physics.Raycast(position, Vector3.down, out RaycastHit hit, float.PositiveInfinity))
                    return hit.point + new Vector3 {y = 0.1f};
            }
            return new Vector3 {y = 8.0f};
        }

        protected override void OnSettingsTick(Container session) { }

        public override bool IsPaused(Container session) => session.Require<VoxelMapNameProperty>() != MapManager.Singleton.Map.name
                                                         || ChunkManager.Singleton.ProgressInfo.stage != MapLoadingStage.Completed;
    }
}