using System.Collections;
using System.Collections.Generic;
using Swihoni.Util.Math;
using Voxel.Map;

namespace Voxel
{
    public class VoxelChangeTransaction : IEnumerable<(Position3Int, VoxelChangeData)>
    {
        private readonly Dictionary<Position3Int, VoxelChangeData> m_Changes;
        private readonly HashSet<Chunk> m_ChunksToUpdate;

        public VoxelChangeTransaction() : this(0) { }

        public VoxelChangeTransaction(int size)
        {
            m_Changes = new Dictionary<Position3Int, VoxelChangeData>(size);
            m_ChunksToUpdate = new HashSet<Chunk>();
        }

        public void AddChange(in Position3Int worldPosition, in VoxelChangeData changeData) => m_Changes.Add(worldPosition, changeData);

        public bool HasChangeAt(in Position3Int worldPosition) => m_Changes.ContainsKey(worldPosition);

        /// <summary>
        /// Apply changes to mesh and clear transaction for reuse.
        /// </summary>
        public void Commit()
        {
            foreach (KeyValuePair<Position3Int, VoxelChangeData> pair in m_Changes)
            {
                Chunk chunk = ChunkManager.Singleton.GetChunkFromWorldPosition(pair.Key);
                if (!chunk) continue;
                Position3Int voxelChunkPosition = ChunkManager.Singleton.WorldVoxelToChunkVoxel(pair.Key, chunk);
                chunk.SetVoxelDataNoCheck(voxelChunkPosition, pair.Value);
                ChunkManager.Singleton.AddChunksToUpdateFromVoxel(voxelChunkPosition, chunk, m_ChunksToUpdate);
                MapManager.Singleton.Map.changedVoxels.SetVoxel(pair.Key, pair.Value);
            }
            foreach (Chunk chunk in m_ChunksToUpdate)
                ChunkManager.UpdateChunkMesh(chunk);
            m_Changes.Clear();
            m_ChunksToUpdate.Clear();
        }

        public IEnumerator<(Position3Int, VoxelChangeData)> GetEnumerator()
        {
            foreach (KeyValuePair<Position3Int, VoxelChangeData> pair in m_Changes)
                yield return (pair.Key, pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}