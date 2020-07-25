using System.Collections;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using UnityEngine.Profiling;
using Voxelation.Map;

namespace Voxelation
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
        private static readonly EvaluatedVoxelsTransaction EvaluationTransaction = new EvaluatedVoxelsTransaction();

        [SerializeField] private GameObject m_ChunkPrefab = default;
        [SerializeField] private int m_ChunkSize = default;
        private readonly Stack<Chunk> m_ChunkPool = new Stack<Chunk>();
        private int m_PoolSize;
        private MapContainer m_LoadedMap;

        public int ChunkSize => m_ChunkSize;
        public MapContainer Map { get; private set; }
        public Dictionary<Position3Int, Chunk> Chunks { get; } = new Dictionary<Position3Int, Chunk>();
        private MapProgressInfo m_Progress = new MapProgressInfo {stage = MapLoadingStage.Completed};
        public MapProgressInfo ProgressInfo
        {
            get => m_Progress;
            private set
            {
                ProgressCallback?.Invoke(value);
                m_Progress = value;
            }
        }

        public MapProgressCallback ProgressCallback { get; set; }

        public IEnumerator LoadMap(MapContainer map)
        {
            Map = map;
            m_LoadedMap = map.Clone();
            SetPoolSize(map);
            // Decommission all current chunks
            yield return DecommissionAllChunks();
            if (map.terrainHeight.WithoutValue) yield break;
            yield return ManagePoolSize();
            // if (!map.DynamicChunkLoading)
            // {
            yield return ChunkActionForAllStaticMapChunks(map, ChunkActionType.Commission);
            yield return ChunkActionForAllStaticMapChunks(map, ChunkActionType.Generate);
            yield return ChunkActionForAllStaticMapChunks(map, ChunkActionType.UpdateMesh);
            // }
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

        private IEnumerator ChunkActionForAllStaticMapChunks(MapContainer map, ChunkActionType actionType)
        {
            var progress = 0.0f;
            Position3Int lower = map.dimension.lowerBound, upper = map.dimension.upperBound;
            for (int x = lower.x; x <= upper.x; x++)
            {
                for (int y = lower.y; y <= upper.y; y++)
                {
                    for (int z = lower.z; z <= upper.z; z++)
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
            if (actionType == ChunkActionType.Generate)
                foreach (KeyValuePair<Position3Int, VoxelChange> pair in map.m_VoxelChanges.Map)
                    EvaluateVoxelChange(pair.Key, pair.Value, updateMesh: false, updateSave: false);
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
                    Destroy(m_ChunkPool.Pop().gameObject);
                yield return null;
            }
        }

        /// <summary>
        /// Change the data of a chunk in the array.
        /// </summary>
        /// <param name="worldPosition">World position of the voxel</param>
        /// <param name="change">Data to change on voxel</param>
        /// <param name="chunk">Chunk that we know it is in. If null, we will try to find it</param>
        /// <param name="updateMesh">Whether or not to actually update the chunk's mesh</param>
        /// <param name="updateSave">Whether or not to update map save component</param>
        /// <param name="existingTransaction">Merge to existing transaction</param>
        public void EvaluateVoxelChange(in Position3Int worldPosition, in VoxelChange change, Chunk chunk = null, bool updateMesh = true, bool updateSave = true,
                                        EvaluatedVoxelsTransaction existingTransaction = null)
        {
            Profiler.BeginSample("Apply Voxel Change");
            Random.InitState(worldPosition.GetHashCode());
            EvaluatedVoxelsTransaction transaction = existingTransaction ?? EvaluationTransaction;
            if (change.magnitude is float radius)
            {
                bool isAdditive = radius > 0.0f;
                float absoluteRadius = Mathf.Abs(radius);
                int roundedRadius = Mathf.CeilToInt(absoluteRadius);
                for (int ix = -roundedRadius; ix <= roundedRadius; ix++)
                for (int iy = -roundedRadius; iy <= roundedRadius; iy++)
                for (int iz = -roundedRadius; iz <= roundedRadius; iz++)
                {
                    Position3Int voxelWorldPosition = worldPosition + new Position3Int(ix, iy, iz);
                    chunk = GetChunkFromWorldPosition(voxelWorldPosition);
                    if (!chunk) continue;
                    Voxel? _voxel = GetVoxel(voxelWorldPosition, chunk);
                    if (!(_voxel is Voxel voxel)) continue;

                    float distance = change.form.GetValueOrDefault() == VoxelVolumeForm.Cylindrical
                        ? Mathf.Sqrt((worldPosition.x - voxelWorldPosition.x).Square() + (worldPosition.z - voxelWorldPosition.z).Square())
                        : Position3Int.Distance(worldPosition, voxelWorldPosition);
                    byte newDensity = checked((byte) Mathf.RoundToInt(Mathf.Clamp01(distance / absoluteRadius * 0.5f * Random.Range(0.8f, 1.0f)) * byte.MaxValue)),
                         currentDensity = voxel.density;
                    VoxelChange evaluatedChange = default;

                    if (voxel.IsUnbreakable) continue;

                    if (isAdditive)
                    {
                        newDensity = checked((byte) (byte.MaxValue - newDensity));
                        if (newDensity > currentDensity)
                        {
                            evaluatedChange.density = newDensity;
                            if (voxel.OnlySmooth) evaluatedChange.Merge(change);
                        }
                    }
                    else
                    {
                        if (newDensity < currentDensity) evaluatedChange.density = newDensity;
                    }
                    if (voxel.HasBlock && !change.modifiesBlocks.GetValueOrDefault()) continue;

                    if (!isAdditive && change.texture is byte texture)
                        evaluatedChange.texture = texture;
                    if (change.color is Color32 color) evaluatedChange.color = Color32.Lerp(color, voxel.color, Mathf.Clamp01(distance / absoluteRadius) * 0.1f);
                    bool inside = distance < roundedRadius;
                    if (!isAdditive && change.modifiesBlocks.GetValueOrDefault() && inside && voxel.HasBlock)
                        evaluatedChange.hasBlock = false;
                    evaluatedChange.natural = false;
                    transaction.Set(voxelWorldPosition, evaluatedChange);
                }
            }
            else
            {
                if (!chunk) chunk = GetChunkFromWorldPosition(worldPosition);
                if (!chunk) return;
                Position3Int voxelChunkPosition = WorldVoxelToChunkVoxel(worldPosition, chunk);
                // ReSharper disable once PossibleNullReferenceException
                chunk.SetVoxelDataNoCheck(voxelChunkPosition, change);
                transaction.Set(worldPosition, change);
            }
            Profiler.EndSample();
            if (updateSave) Map.m_VoxelChanges.Set(worldPosition, change);
            if (updateMesh) transaction.Commit();
        }

        public VoxelChange? GetMapSaveVoxel(in Position3Int worldPosition)
        {
            Chunk chunk = GetChunkFromWorldPosition(worldPosition);
            if (!chunk) return null;
            Position3Int voxelChunkPosition = WorldVoxelToChunkVoxel(worldPosition, chunk);
            VoxelChange change = chunk.GetChangeDataFromSave(voxelChunkPosition, m_LoadedMap);
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
            // ReSharper disable once PossibleNullReferenceException
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

        public static void UpdateChunkMesh(Chunk chunk) => chunk.UpdateAndApply();

        public void AddChunksToUpdateFromVoxel(in Position3Int voxelChunkPosition, Chunk originatingChunk, ICollection<Chunk> chunksToUpdate)
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

        private static readonly HashSet<Chunk> UpdateAdjacentChunks = new HashSet<Chunk>();

        private void UpdateChunkMesh(Chunk chunk, in Position3Int voxelChunkPosition)
        {
            AddChunksToUpdateFromVoxel(voxelChunkPosition, chunk, UpdateAdjacentChunks);
            foreach (Chunk chunkToUpdate in UpdateAdjacentChunks)
                UpdateChunkMesh(chunkToUpdate);
            UpdateAdjacentChunks.Clear();
        }

        public Chunk GetChunkFromPosition(in Position3Int chunkPosition)
        {
            Chunks.TryGetValue(chunkPosition, out Chunk containerChunk);
            return containerChunk;
        }

        private void SetPoolSize(MapContainer save)
        {
            Position3Int upper = save.dimension.upperBound, lower = save.dimension.lowerBound;
            m_PoolSize = (upper.x - lower.x + 1) * (upper.y - lower.y + 1) * (upper.z - lower.z + 1);
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

        public Position3Int WorldVoxelToChunkVoxel(in Position3Int worldPosition, Chunk chunk) => worldPosition - chunk.Position * m_ChunkSize;

        /// <summary>
        /// Given a world position, return the position of the chunk that would contain it.
        /// </summary>
        /// <param name="worldPosition">World position inside of chunk</param>
        /// <returns>Position of chunk in respect to chunks dictionary</returns>
        private Position3Int WorldToChunk(in Vector3 worldPosition)
        {
            float chunkSize = m_ChunkSize;
            return new Position3Int(Mathf.FloorToInt(worldPosition.x / chunkSize),
                                    Mathf.FloorToInt(worldPosition.y / chunkSize),
                                    Mathf.FloorToInt(worldPosition.z / chunkSize));
        }
    }
}