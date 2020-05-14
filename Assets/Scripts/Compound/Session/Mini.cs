using Swihoni.Sessions;
using Swihoni.Util.Math;
using Voxel;

namespace Compound.Session
{
    public abstract class MiniBase
    {
        protected readonly SessionBase m_Session;

        protected MiniBase(SessionBase session) => m_Session = session;

        public virtual void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData change, Chunk chunk = null, bool updateMesh = true)
            => ChunkManager.Singleton.SetVoxelData(worldPosition, change, chunk, updateMesh);

        public virtual void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, ChangedVoxelsProperty changedVoxels = null)
            => ChunkManager.Singleton.RemoveVoxelRadius(worldPosition, radius, replaceGrassWithDirt, changedVoxels);
    }

    public interface IMiniProvider
    {
        MiniBase GetMini();
    }
}