using Swihoni.Util.Math;
using Voxel;

namespace Compound.Session
{
    public abstract class MiniBase
    {
        public virtual void SetVoxelData(in Position3Int worldPosition, in VoxelChangeData changeData, Chunk chunk = null, bool updateMesh = true)
            => ChunkManager.Singleton.SetVoxelData(worldPosition, changeData, chunk, updateMesh);

        public virtual void RemoveVoxelRadius(Position3Int worldPosition, float radius, bool replaceGrassWithDirt = false, in VoxelChangeMap changedVoxels = null)
            => ChunkManager.Singleton.RemoveVoxelRadius(worldPosition, radius, replaceGrassWithDirt, changedVoxels);
    }

    public interface IMiniProvider
    {
        MiniBase GetMini();
    }
}