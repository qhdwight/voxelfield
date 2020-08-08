using System.Collections.Generic;

namespace Voxels
{
    public class TouchedChunks : HashSet<Chunk>
    {
        public void UpdateMesh()
        {
            foreach (Chunk chunk in this)
                chunk.UpdateAndApply();
            Clear();
        }
    }

    // public class EvaluatedVoxelsTransaction : VoxelChangesProperty
    // {
    //     private readonly HashSet<Chunk> m_ChunksToUpdate = new HashSet<Chunk>();
    //
    //     /// <summary>
    //     /// Apply changes to mesh and clear transaction for reuse.
    //     /// </summary>
    //     public void Commit(bool updateMesh = true)
    //     {
    //         foreach (KeyValuePair<Position3Int, VoxelChange> pair in m_Map)
    //         {
    //             Chunk chunk = ChunkManager.Singleton.GetChunkFromWorldPosition(pair.Key);
    //             if (!chunk) continue;
    //             Position3Int voxelChunkPosition = ChunkManager.Singleton.WorldVoxelToChunkVoxel(pair.Key, chunk);
    //             chunk.SetVoxelDataNoCheck(voxelChunkPosition, pair.Value);
    //             if (updateMesh) ChunkManager.Singleton.AddChunksToUpdateFromVoxel(voxelChunkPosition, chunk, m_ChunksToUpdate);
    //         }
    //         if (updateMesh)
    //         {
    //             foreach (Chunk chunk in m_ChunksToUpdate)
    //                 ChunkManager.UpdateChunkMesh(chunk);
    //             m_ChunksToUpdate.Clear();
    //         }
    //         Clear();
    //     }
    // }
}