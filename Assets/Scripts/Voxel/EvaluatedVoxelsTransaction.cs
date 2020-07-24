using System.Collections.Generic;
using Swihoni.Util.Math;
using Voxel.Map;

namespace Voxel
{
    public class EvaluatedVoxelsTransaction : VoxelChangesProperty
    {
        private readonly HashSet<Chunk> m_ChunksToUpdate = new HashSet<Chunk>();

        /// <summary>
        /// Apply changes to mesh and clear transaction for reuse.
        /// </summary>
        public void Commit()
        {
            foreach (KeyValuePair<Position3Int, VoxelChange> pair in m_Map)
            {
                Chunk chunk = ChunkManager.Singleton.GetChunkFromWorldPosition(pair.Key);
                if (!chunk) continue;
                Position3Int voxelChunkPosition = ChunkManager.Singleton.WorldVoxelToChunkVoxel(pair.Key, chunk);
                chunk.SetVoxelDataNoCheck(voxelChunkPosition, pair.Value);
                ChunkManager.Singleton.AddChunksToUpdateFromVoxel(voxelChunkPosition, chunk, m_ChunksToUpdate);
                MapManager.Singleton.Map.m_VoxelChanges.Set(pair.Key, pair.Value);
            }
            foreach (Chunk chunk in m_ChunksToUpdate)
                ChunkManager.UpdateChunkMesh(chunk);
            Clear();
            m_ChunksToUpdate.Clear();
        }
    }
}