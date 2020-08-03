using System;
using System.Collections;
using System.Collections.Generic;
using Swihoni.Components;
using Swihoni.Util;
using Swihoni.Util.Math;
using Unity.Profiling;
using UnityEngine;
using Voxels.Map;
using Random = UnityEngine.Random;

namespace Voxels
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
            for (int y = lower.y; y <= upper.y; y++)
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
            if (actionType == ChunkActionType.Generate)
                foreach (VoxelChange change in map.voxelChanges.List)
                    ApplyVoxelChanges(change, false, TouchedChunks);
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

        private static readonly TouchedChunks TouchedChunks = new TouchedChunks();

        private static readonly ProfilerMarker ApplyVoxelChangeMarker = new ProfilerMarker("Apply Voxel Change");

        /// <summary>
        /// Change the data of a chunk in the array.
        /// </summary>
        /// <param name="change">Data to change on voxel</param>
        /// <param name="updateSave">Whether or not to update map save component</param>
        /// <param name="existingTouched"></param>
        /// <param name="overrideBreakable"></param>
        public void ApplyVoxelChanges(in VoxelChange change, bool updateSave = true, TouchedChunks existingTouched = null, bool overrideBreakable = false)
        {
            using (ApplyVoxelChangeMarker.Auto())
            {
                Position3Int worldPosition = change.position.Value;
                Random.InitState(worldPosition.GetHashCode());
                TouchedChunks touched = existingTouched ?? TouchedChunks;

                VoxelChange GetEvaluated(in VoxelChange originalChange, Chunk chunk, in Position3Int voxelChunkPosition, bool mergeOriginal = true)
                {
                    VoxelChange evaluatedChange = originalChange.revert.GetValueOrDefault() ? chunk.GetChangeFromMap(voxelChunkPosition, Map) : default;
                    if (mergeOriginal) evaluatedChange.Merge(originalChange);
                    return evaluatedChange;
                }
                
                void SetEvaluatedVoxel(in VoxelChange originalChange, in VoxelChange evaluatedChange, Chunk chunk, in Position3Int voxelChunkPosition)
                {
                    originalChange.undo?.Add(chunk.GetVoxelNoCheck(voxelChunkPosition));
                    chunk.SetVoxelDataNoCheck(voxelChunkPosition, evaluatedChange);
                    AddChunksToUpdateFromVoxel(voxelChunkPosition, chunk, touched);
                }

                VoxelVolumeForm form = change.form.Value;
                switch (form)
                {
                    case VoxelVolumeForm.Single:
                    {
                        Chunk chunk = GetChunkFromWorldPosition(worldPosition);
                        if (!chunk) return;

                        Position3Int voxelChunkPosition = WorldVoxelToChunkVoxel(worldPosition, chunk);
                        VoxelChange evaluatedChange = GetEvaluated(change, chunk, voxelChunkPosition);
                        SetEvaluatedVoxel(change, evaluatedChange, chunk, voxelChunkPosition);
                        break;
                    }
                    case VoxelVolumeForm.Prism:
                    {
                        Position3Int l = worldPosition, u = change.upperBound.Value;
                        for (int x = Math.Min(l.x, u.x); x <= Math.Max(l.x, u.x); x++)
                        for (int y = Math.Min(l.y, u.y); y <= Math.Max(l.y, u.y); y++)
                        for (int z = Math.Min(l.z, u.z); z <= Math.Max(l.z, u.z); z++)
                        {
                            var voxelWorldPosition = new Position3Int(x, y, z);
                            Chunk chunk = GetChunkFromWorldPosition(voxelWorldPosition);
                            if (!chunk) continue;

                            Position3Int voxelChunkPosition = WorldVoxelToChunkVoxel(voxelWorldPosition, chunk);

                            /* Evaluated Change */
                            VoxelChange evaluatedChange = GetEvaluated(change, chunk, voxelChunkPosition);
                            evaluatedChange.form = VoxelVolumeForm.Single;
                            SetEvaluatedVoxel( change, evaluatedChange, chunk, voxelChunkPosition);
                        }
                        break;
                    }
                    case VoxelVolumeForm.Spherical:
                    case VoxelVolumeForm.Cylindrical:
                    {
                        float radius = change.magnitude.Value;
                        bool isAdditive = radius > 0.0f;
                        float absoluteRadius = Mathf.Abs(radius);
                        int roundedRadius = Mathf.CeilToInt(absoluteRadius);
                        for (int ix = -roundedRadius; ix <= roundedRadius; ix++)
                        for (int iy = -roundedRadius; iy <= roundedRadius; iy++)
                        for (int iz = -roundedRadius; iz <= roundedRadius; iz++)
                        {
                            Position3Int voxelWorldPosition = worldPosition + new Position3Int(ix, iy, iz);
                            Chunk chunk = GetChunkFromWorldPosition(voxelWorldPosition);
                            if (!chunk) continue;

                            Position3Int voxelChunkPosition = WorldVoxelToChunkVoxel(voxelWorldPosition, chunk);
                            ref Voxel voxel = ref chunk.GetVoxelNoCheck(voxelChunkPosition);

                            float GetDistance(VoxelVolumeForm _form, in Vector3 _center, in Vector3 _voxel) => _form == VoxelVolumeForm.Cylindrical
                                ? Mathf.Sqrt((_center.x - _voxel.x).Square() + (_center.z - _voxel.z).Square())
                                : Vector3.Distance(_center, _voxel);

                            float distance = GetDistance(form, worldPosition, voxelWorldPosition),
                                  floatDensity = distance / absoluteRadius * 0.5f;
                            if (!change.noRandom.GetValueOrDefault()) floatDensity *= Random.Range(0.85f, 1.0f);
                            byte newDensity = checked((byte) Mathf.RoundToInt(Mathf.Clamp01(floatDensity) * byte.MaxValue)),
                                 currentDensity = voxel.density;
                            
                            if (!overrideBreakable && voxel.IsUnbreakable) continue;

                            /* Evaluated Change */
                            VoxelChange evaluatedChange = GetEvaluated(change, chunk, voxelChunkPosition, false);
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

                            bool isInside = GetDistance(form, worldPosition, voxelWorldPosition + new Vector3(0.5f, 0.5f, 0.5f)) < absoluteRadius * 1.5f;

                            if (isInside)
                            {
                                if (!isAdditive && change.modifiesBlocks.GetValueOrDefault() && voxel.HasBlock)
                                    evaluatedChange.hasBlock = false;
                                if (change.color is Color32 color)
                                    evaluatedChange.color = Color32.Lerp(color, voxel.color, Mathf.Clamp01(distance / absoluteRadius) * 0.1f);
                                if (change.texture is byte texture)
                                    evaluatedChange.texture = texture;
                            }
                            evaluatedChange.natural = false;
                            evaluatedChange.form = VoxelVolumeForm.Single;
                            SetEvaluatedVoxel( change, evaluatedChange, chunk, voxelChunkPosition);
                        }
                        break;
                    }
                    case VoxelVolumeForm.Wall:
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(form), form, null);
                }
                if (existingTouched is null) TouchedChunks.UpdateMesh();
                if (updateSave) Map.voxelChanges.Add(change);
            }
        }

        public VoxelChange? GetMapSaveVoxel(in Position3Int worldPosition)
        {
            Chunk chunk = GetChunkFromWorldPosition(worldPosition);
            if (!chunk) return null;
            Position3Int voxelChunkPosition = WorldVoxelToChunkVoxel(worldPosition, chunk);
            VoxelChange change = chunk.GetChangeFromMap(voxelChunkPosition, m_LoadedMap);
            change.position = worldPosition;
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