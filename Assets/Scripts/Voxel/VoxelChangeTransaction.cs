using System.Collections;
using System.Collections.Generic;
using Swihoni.Util.Math;

namespace Voxel
{
    public class VoxelChangeTransaction : IEnumerable<(Position3Int, VoxelChangeData)>
    {
        private readonly Dictionary<Position3Int, VoxelChangeData> m_ChangeData;
        private readonly HashSet<Chunk> m_ChunksToUpdate;

        public VoxelChangeTransaction() : this(0) { }

        public VoxelChangeTransaction(int size)
        {
            m_ChangeData = new Dictionary<Position3Int, VoxelChangeData>(size);
            m_ChunksToUpdate = new HashSet<Chunk>();
        }

        public void AddChange(in Position3Int worldPosition, in VoxelChangeData changeData) => m_ChangeData.Add(worldPosition, changeData);

        public bool HasChangeAt(in Position3Int worldPosition) => m_ChangeData.ContainsKey(worldPosition);

        /// <summary>
        /// Apply changes to mesh and clear transaction for reuse.
        /// </summary>
        public void Commit()
        {
            foreach (KeyValuePair<Position3Int, VoxelChangeData> changeData in m_ChangeData)
            {
                Chunk chunk = ChunkManager.Singleton.GetChunkFromWorldPosition(changeData.Key);
                if (!chunk) continue;
                Position3Int voxelChunkPosition = ChunkManager.Singleton.WorldVoxelToChunkVoxel(changeData.Key, chunk);
                chunk.SetVoxelDataNoCheck(voxelChunkPosition, changeData.Value);
                ChunkManager.Singleton.AddChunksToUpdateFromVoxel(voxelChunkPosition, chunk, m_ChunksToUpdate);
            }
            foreach (Chunk chunk in m_ChunksToUpdate)
                ChunkManager.UpdateChunkMesh(chunk);
            m_ChangeData.Clear();
            m_ChunksToUpdate.Clear();
        }

        public IEnumerator<(Position3Int, VoxelChangeData)> GetEnumerator()
        {
            foreach (KeyValuePair<Position3Int, VoxelChangeData> pair in m_ChangeData)
                yield return (pair.Key, pair.Value);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}