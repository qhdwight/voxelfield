using System.Collections;
using System.Collections.Generic;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using Voxel.Map;

namespace Voxel
{
    public delegate void MapProgressCallback(in MapProgressInfo mapProgressInfoCallback);

    public enum MapLoadingStage
    {
        CleaningUp,
        SettingUp,
        Generating,
        UpdatingMesh,
        Completed
    }

    public struct MapProgressInfo
    {
        public MapLoadingStage stage;
        public float progress;
    }

    public enum ChunkActionType
    {
        Commission,
        Generate,
        UpdateMesh
    }

    public class ChunkManager : SingletonBehavior<ChunkManager>
    {
        private static readonly VoxelChangeTransaction Transaction = new VoxelChangeTransaction();

        [SerializeField] private GameObject m_ChunkPrefab = default;
        [SerializeField] private int m_ChunkSize = default;
        private readonly Stack<Chunk> m_ChunkPool = new Stack<Chunk>();
        private int m_PoolSize;

        public int ChunkSize => m_ChunkSize;
        public MapSave Map { get; private set; }
        public Dictionary<Position3Int, Chunk> Chunks { get; } = new Dictionary<Position3Int, Chunk>();
        private MapProgressInfo m_Progress;
        public MapProgressInfo ProgressInfo
        {
            get => m_Progress;
            set
            {
                ProgressCallback?.Invoke(value);
                m_Progress = value;
            }
        }

        public MapProgressCallback ProgressCallback { get; set; }

        public IEnumerator LoadMap(MapSave map)
        {
            Map = map;
            SetPoolSize(map);
            // Decommission all current chunks
            yield return DecommissionAllChunks();
            if (!map.TerrainGenerationData.HasValue) yield break;
            yield return ManagePoolSize();
            if (!map.DynamicChunkLoading)
            {
                yield return ChunkActionForAllStaticMapChunks(map, ChunkActionType.Commission);
                yield return ChunkActionForAllStaticMapChunks(map, ChunkActionType.Generate);
                yield return ChunkActionForAllStaticMapChunks(map, ChunkActionType.UpdateMesh);
            }
            ProgressInfo = new MapProgressInfo {stage = MapLoadingStage.Completed};
        }

        private IEnumerator DecommissionAllChunks()
        {
            var progress = 0.0f;
            int commissionedChunks = Chunks.Count;
            var chunksInPositionToRemove = new List<Position3Int>(Chunks.Keys);
            foreach (Position3Int chunkPosition in chunksInPositionToRemove)
            {
                DecommissionChunkInPosition(chunkPosition);
                progress += 1.0f / commissionedChunks;
                var progressInfo = new MapProgressInfo {stage = MapLoadingStage.CleaningUp, progress = progress};
                ProgressCallback?.Invoke(progressInfo);
                yield return null;
            }
        }

        private IEnumerator ChunkActionForAllStaticMapChunks(MapSave map, ChunkActionType actionType)
        {
            var progress = 0.0f;
            for (int x = map.Dimension.lowerBound.x; x <= map.Dimension.upperBound.x; x++)
            {
                for (int y = map.Dimension.lowerBound.y; y <= map.Dimension.upperBound.y; y++)
                {
                    for (int z = map.Dimension.lowerBound.z; z <= map.Dimension.upperBound.z; z++)
                    {
                        var chunkPosition = new Position3Int(x, y, z);
                        progress += 1.0f / m_PoolSize;
                        var progressInfo = new MapProgressInfo {progress = progress};
                        switch (actionType)
                        {
                            case ChunkActionType.Commission:
                            {
                                CommissionChunkFromPoolIntoPosition(chunkPosition);
                                progressInfo.stage = MapLoadingStage.SettingUp;
                                break;
                            }
                            case ChunkActionType.Generate:
                            {
                                GetChunkFromPosition(chunkPosition).CreateTerrainFromSave(map);
                                progressInfo.stage = MapLoadingStage.Generating;
                                break;
                            }
                            case ChunkActionType.UpdateMesh:
                            {
                                UpdateChunkMesh(GetChunkFromPosition(chunkPosition));
                                progressInfo.stage = MapLoadingStage.UpdatingMesh;
                                break;
                            }
                            default:
                            {
                                Debug.LogWarning($"Unrecognized chunk action type {actionType}");
                                break;
                            }
                        }
                        ProgressInfo = progressInfo;
                        yield return null;
                    }
                }
            }
        }

        /// <summary>
        /// Manage the size of the pool, commission chunks if there are too little,
        /// and decommission if there are too much.
        /// This will only change things if chunk pool and the chunks are free.
        /// </summary>
        private IEnumerator ManagePoolSize()
        {
            int totalAmountOfChunks;
            while ((totalAmountOfChunks = m_ChunkPool.Count + Chunks.Count) != m_PoolSize)
            {
                if (totalAmountOfChunks < m_PoolSize)
                {
                    GameObject chunkInstance = Instantiate(m_ChunkPrefab);
                    chunkInstance.name = "DecommissionedChunk";
                    // chunkInstance.hideFlags = HideFlags.HideInHierarchy;
                    var chunk = chunkInstance.GetComponent<Chunk>();
                    chunk.Initialize(this, m_ChunkSize);
                    m_ChunkPool.Push(chunk);
                }
                else if (totalAmountOfChunks > m_PoolSize)
                {
                    Destroy(m_ChunkPool.Pop().gameObject);
                }
                yield return null;
            }
        }

        /// <summary>
        /// Change the data of a chunk in the array.
        /// </summary>
        /// <param name="worldPosition">World position of the voxel</param>
        /// <param name="changeData">Data to change on voxel</param>
        /// <param name="chunk">Chunk that we know it is in. If null, we will try to find it</param>
        /// <param name="updateMesh">Whether or not to actually update the chunk's mesh</param>
        public void SetVoxelData
            (in Position3Int worldPosition, VoxelChangeData changeData, Chunk chunk = null, bool updateMesh = true)
        {
            if (!chunk) chunk = GetChunkFromWorldPosition(worldPosition);
            if (!chunk) return;
            Position3Int voxelChunkPosition = WorldVoxelToChunkVoxel(worldPosition, chunk);
            chunk.SetVoxelDataNoCheck(voxelChunkPosition, changeData);
            if (updateMesh) UpdateChunkMesh(chunk, voxelChunkPosition);
        }

        public VoxelChangeData? RevertVoxelToMapSave(in Position3Int worldPosition, bool updateMesh = true)
        {
            Chunk chunk = GetChunkFromWorldPosition(worldPosition);
            if (!chunk) return null;
            Position3Int voxelChunkPosition = WorldVoxelToChunkVoxel(worldPosition, chunk);
            VoxelChangeData? change = chunk.RevertToMapSave(voxelChunkPosition, Map);
            if (updateMesh) UpdateChunkMesh(chunk, voxelChunkPosition);
            return change;
        }

        /// <summary>
        /// Given a world position, find the chunk and then the voxel in that chunk.
        /// </summary>
        /// <param name="worldPosition">Position of voxel in world space</param>
        /// <param name="chunk">Chunk we know it is in. If it is null, we will try to find it</param>
        /// <returns>Voxel in that chunk, or null if it does not exist</returns>
        public Voxel? GetVoxel(in Position3Int worldPosition, Chunk chunk = null)
        {
            if (!chunk) chunk = GetChunkFromWorldPosition(worldPosition);
            if (chunk) return chunk.GetVoxelNoCheck(WorldVoxelToChunkVoxel(worldPosition, chunk));
            return null;
        }

        /// <summary>
        /// Given a world position, determine if a chunk is there.
        /// </summary>
        /// <param name="worldPosition">World position of chunk</param>
        /// <returns>Chunk instance, or null if it doe not exist</returns>
        public Chunk GetChunkFromWorldPosition(in Position3Int worldPosition)
        {
            Position3Int chunkPosition = WorldToChunk(worldPosition);
            return GetChunkFromPosition(chunkPosition);
        }

        public static void UpdateChunkMesh(Chunk chunk) { chunk.UpdateAndApply(); }

        public void AddChunksToUpdateFromVoxel
            (in Position3Int voxelChunkPosition, Chunk originatingChunk, ICollection<Chunk> chunksToUpdate)
        {
            chunksToUpdate.Add(originatingChunk);

            void AddIfNeeded(int voxelChunkPositionSingle, in Position3Int axis)
            {
                int sign;
                if (voxelChunkPositionSingle == 0) sign = -1;
                else if (voxelChunkPositionSingle == m_ChunkSize - 1) sign = 1;
                else return;
                Chunk chunk = GetChunkFromPosition(originatingChunk.Position + axis * sign);
                if (chunk) chunksToUpdate.Add(chunk);
            }

            AddIfNeeded(voxelChunkPosition.x, new Position3Int {x = 1});
            AddIfNeeded(voxelChunkPosition.y, new Position3Int {y = 1});
            AddIfNeeded(voxelChunkPosition.z, new Position3Int {z = 1});
        }

        private static readonly HashSet<Chunk> SUpdateAdjacentChunks = new HashSet<Chunk>();

        private void UpdateChunkMesh(Chunk chunk, in Position3Int voxelChunkPosition)
        {
            AddChunksToUpdateFromVoxel(voxelChunkPosition, chunk, SUpdateAdjacentChunks);
            foreach (Chunk chunkToUpdate in SUpdateAdjacentChunks)
                UpdateChunkMesh(chunkToUpdate);
            SUpdateAdjacentChunks.Clear();
        }

        public Chunk GetChunkFromPosition(in Position3Int chunkPosition)
        {
            Chunks.TryGetValue(chunkPosition, out Chunk containerChunk);
            return containerChunk;
        }

        private void SetPoolSize(MapSave save)
        {
            Dimension dimension = save.Dimension;
            m_PoolSize = (dimension.upperBound.x - dimension.lowerBound.x + 1) *
                         (dimension.upperBound.y - dimension.lowerBound.y + 1) *
                         (dimension.upperBound.z - dimension.lowerBound.z + 1);
        }

        private void CommissionChunkFromPoolIntoPosition(in Position3Int newChunkPosition)
        {
            if (Chunks.ContainsKey(newChunkPosition)) return;
            Chunk chunk = m_ChunkPool.Pop();
            Chunks.Add(newChunkPosition, chunk);
            chunk.Commission(newChunkPosition);
        }

        private void DecommissionChunkInPosition(in Position3Int chunkPosition)
        {
            Chunk chunk = GetChunkFromPosition(chunkPosition);
            if (!chunk) return;
            chunk.Decommission();
            Chunks.Remove(chunk.Position);
            m_ChunkPool.Push(chunk);
        }

        public void RemoveVoxelRadius
            (in Position3Int worldPositionCenter, float radius, bool replaceGrassWithDirt = false, VoxelChangeMap changedVoxels = null)
        {
            int roundedRadius = Mathf.CeilToInt(radius);
            for (int ix = -roundedRadius; ix <= roundedRadius; ix++)
            {
                for (int iy = -roundedRadius; iy <= roundedRadius; iy++)
                {
                    for (int iz = -roundedRadius; iz <= roundedRadius; iz++)
                    {
                        Position3Int voxelWorldPosition = worldPositionCenter + new Position3Int(ix, iy, iz);
                        Chunk chunk = GetChunkFromWorldPosition(voxelWorldPosition);
                        if (!chunk) continue;
                        Voxel? voxel = GetVoxel(voxelWorldPosition, chunk);
                        if (!voxel.HasValue) continue;
                        float distance = Position3Int.Distance(worldPositionCenter, voxelWorldPosition);
//                        if (distance > roundedRadius) continue;
                        byte newDensity = (byte) Mathf.RoundToInt(Mathf.Clamp01(distance / radius * 0.5f) * byte.MaxValue),
                             currentDensity = voxel.Value.density;
                        if (newDensity >= currentDensity) continue;
                        var changeData = new VoxelChangeData {density = newDensity};
                        if (replaceGrassWithDirt && voxel.Value.texture == VoxelTexture.Grass) changeData.texture = VoxelTexture.Dirt;
                        changedVoxels?.Set(voxelWorldPosition, changeData);
                        if (!Transaction.HasChangeAt(voxelWorldPosition)) Transaction.AddChange(voxelWorldPosition, changeData);
                    }
                }
            }
            Transaction.Commit();
        }

        public Position3Int WorldVoxelToChunkVoxel(in Position3Int worldPosition, Chunk chunk) => worldPosition - chunk.Position * m_ChunkSize;

        /// <summary>
        /// Given a world position, return the position of the chunk that would contain it.
        /// </summary>
        /// <param name="worldPosition">World position inside of chunk</param>
        /// <returns>Position of chunk in respect to chunks dictionary</returns>
        private Position3Int WorldToChunk(in Vector3 worldPosition)
        {
            float multiple = m_ChunkSize;
            return new Position3Int(Mathf.FloorToInt(worldPosition.x / multiple),
                                    Mathf.FloorToInt(worldPosition.y / multiple),
                                    Mathf.FloorToInt(worldPosition.z / multiple));
        }
    }
}