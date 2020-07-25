using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using Voxel.Map;

namespace Voxel
{
    [RequireComponent(typeof(MeshCollider), typeof(MeshRenderer), typeof(MeshFilter))]
    public class Chunk : MonoBehaviour
    {
        [SerializeField] private MeshFilter m_SolidMeshFilter = default;
        [SerializeField] private Material m_FoliageMaterial = default;
        [SerializeField, Layer] private int m_Layer = default;

        private readonly MeshData m_SolidMeshData = new MeshData(), m_FoliageMeshData = new MeshData();

        private ChunkManager m_ChunkManager;
        private Mesh m_SolidMesh, m_FoliageMesh;
        private MeshRenderer[] m_Renderers;
        private Position3Int m_Position;
        private bool m_InCommission, m_Generating, m_Updating;
        private int m_ChunkSize;
        private Voxel[] m_Voxels;

        public MeshCollider MeshCollider { get; private set; }
        public ref Position3Int Position => ref m_Position;

        public override int GetHashCode() => m_Position.GetHashCode();

        public override bool Equals(object other) => other is Chunk && other.GetHashCode() == GetHashCode();

        private void Awake()
        {
            m_SolidMesh = m_SolidMeshFilter.mesh;
            m_SolidMesh.indexFormat = IndexFormat.UInt32;
            m_FoliageMesh = new Mesh {indexFormat = IndexFormat.UInt32};
            MeshCollider = GetComponent<MeshCollider>();
            m_Renderers = GetComponentsInChildren<MeshRenderer>();
            m_SolidMesh.MarkDynamic();
            m_FoliageMesh.MarkDynamic();
        }

        private void Update()
        {
            if (m_InCommission)
                Graphics.DrawMesh(m_FoliageMesh, transform.position, Quaternion.identity, m_FoliageMaterial, m_Layer,
                                  null, 0, null, false, true);
        }

        public void Initialize(ChunkManager chunkManager, int chunkSize)
        {
            m_ChunkManager = chunkManager;
            m_ChunkSize = chunkSize;
            m_Voxels = new Voxel[m_ChunkSize * m_ChunkSize * m_ChunkSize];
        }

        public void Decommission()
        {
            SetCommission(false);
            gameObject.name = "DecommissionedChunk";
        }

        public void Commission(in Position3Int position)
        {
            SetCommission(true);
            m_Position = position;
            transform.position = m_Position * m_ChunkSize;
            gameObject.name = $"Chunk{position}";
        }

        private void SetCommission(bool inCommission)
        {
            ClearMeshes();
            m_SolidMeshData.Clear();
            m_InCommission = inCommission;
            foreach (MeshRenderer meshRenderer in m_Renderers) meshRenderer.enabled = m_InCommission;
        }

        private void ClearMeshes()
        {
            m_SolidMesh.Clear();
            if (MeshCollider.sharedMesh) MeshCollider.sharedMesh.Clear();
        }

        public bool InsideChunk(in Position3Int pos) => pos.x < m_ChunkSize && pos.y < m_ChunkSize && pos.z < m_ChunkSize
                                                      && pos.x >= 0 && pos.y >= 0 && pos.z >= 0;

        public void SetVoxelDataNoCheck(in Position3Int position, in VoxelChange change)
            => m_Voxels?[position.z + m_ChunkSize * (position.y + m_ChunkSize * position.x)].SetVoxelData(change);

        public Voxel? GetVoxel(in Position3Int internalPosition)
        {
            return InsideChunk(internalPosition)
                ? GetVoxelNoCheck(internalPosition)
                : m_ChunkManager.GetVoxel(internalPosition + m_Position * m_ChunkSize);
        }

        public ref Voxel GetVoxelNoCheck(in Position3Int position)
            => ref m_Voxels[position.z + m_ChunkSize * (position.y + m_ChunkSize * position.x)];

        public ref Voxel GetVoxelNoCheck(int index) => ref m_Voxels[index];

        public VoxelChange GetChangeDataFromSave(in Position3Int voxelPosition, MapContainer save)
        {
            float
                noiseHeight = Noise.Simplex(m_Position.x * m_ChunkSize + voxelPosition.x,
                                            m_Position.z * m_ChunkSize + voxelPosition.z, save.noise),
                height = noiseHeight + save.terrainHeight - voxelPosition.y - m_Position.y * m_ChunkSize,
                floatDensity = Mathf.Clamp(height, 0.0f, 2.0f);
            var density = (byte) (floatDensity * byte.MaxValue / 2.0f);
            // TODO:bug discrepancy between blocks and smooth causes lower edges to have one block less modifiable
            bool breakable = save.breakableEdges || !(m_Position.x == save.dimension.lowerBound.Value.x && voxelPosition.x <= 1
                                                   || m_Position.x == save.dimension.upperBound.Value.x && voxelPosition.x == m_ChunkSize - 1
                                                   || m_Position.y == save.dimension.lowerBound.Value.y && voxelPosition.y <= 1
                                                   || m_Position.y == save.dimension.upperBound.Value.y && voxelPosition.y == m_ChunkSize - 1
                                                   || m_Position.z == save.dimension.lowerBound.Value.z && voxelPosition.z <= 1
                                                   || m_Position.z == save.dimension.upperBound.Value.z && voxelPosition.z == m_ChunkSize - 1);
            bool isStone = height > 5.0f;
            return new VoxelChange
            {
                texture = isStone ? VoxelTexture.Checkered : VoxelTexture.Solid,
                hasBlock = false, density = density, isBreakable = breakable, orientation = Orientation.None, natural = true,
                color = isStone ? Voxel.Stone : Voxel.Grass
            };
        }

        public void CreateTerrainFromSave(MapContainer save)
        {
            m_Generating = true;
            for (var x = 0; x < m_ChunkSize; x++)
            for (var z = 0; z < m_ChunkSize; z++)
            for (var y = 0; y < m_ChunkSize; y++)
            {
                var position = new Position3Int(x, y, z);
                VoxelChange change = GetChangeDataFromSave(position, save);
                SetVoxelDataNoCheck(position, change);
            }
            m_Generating = false;
        }

        private void OnDrawGizmosSelected()
        {
            Gizmos.color = m_Generating ? Color.yellow : m_Updating ? Color.red : Color.cyan;
            Gizmos.DrawWireCube(m_Position * m_ChunkSize + Vector3.one * (m_ChunkSize / 2.0f - 0.5f),
                                Vector3.one * m_ChunkSize);
        }

        public void UpdateAndApply()
        {
            UpdateMesh();
            ApplyMesh();
        }

        public void UpdateMesh()
        {
            Profiler.BeginSample("Update Mesh");
            m_Updating = true;
            m_SolidMeshData.Clear();
            m_FoliageMeshData.Clear();
            VoxelRenderer.RenderVoxels(m_ChunkManager, this, m_SolidMeshData, m_FoliageMeshData);
            m_Updating = false;
            Profiler.EndSample();
        }

        private void ApplyMesh()
        {
            Profiler.BeginSample("Apply Mesh");
            ApplyMesh(m_SolidMesh, m_SolidMeshData);
            ApplyMesh(m_FoliageMesh, m_FoliageMeshData);
            MeshCollider.sharedMesh = m_SolidMesh;
            Profiler.EndSample();
        }

        private static void ApplyMesh(Mesh mesh, MeshData data)
        {
            Profiler.BeginSample("Set General");
            mesh.Clear();
            mesh.SetVertices(data.vertices);
            mesh.SetIndices(data.triangleIndices, MeshTopology.Triangles, 0);
            mesh.SetUVs(0, data.uvs);
            mesh.SetColors(data.colors);
            Profiler.EndSample();
            if (data.normals.Count == 0) mesh.RecalculateNormals();
            else mesh.SetNormals(data.normals);
            // Profiler.BeginSample("Calculate Tangents");
            // mesh.RecalculateTangents();
            // Profiler.EndSample();
        }
    }
}