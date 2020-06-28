using Swihoni.Util;
using Swihoni.Util.Math;
using UnityEngine;
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

        private ChunkManager m_ChunkManager;
        private MeshCollider m_MeshCollider;
        private Mesh m_SolidMesh, m_FoliageMesh;
        private MeshRenderer[] m_Renderers;
        private Position3Int m_Position;
        private bool m_InCommission, m_Generating, m_Updating;
        private int m_ChunkSize;

        private Voxel[,,] m_Voxels;

        //private bool m_WaitingForMeshData = true;
        private readonly MeshData m_SolidMeshData = new MeshData(), m_FoliageMeshData = new MeshData();

        public Position3Int Position => m_Position;

        public override int GetHashCode() => m_Position.GetHashCode();

        public override bool Equals(object other) => other is Chunk && other.GetHashCode() == GetHashCode();

        private void Awake()
        {
            m_SolidMesh = m_SolidMeshFilter.mesh;
            m_SolidMesh.indexFormat = IndexFormat.UInt32;
            m_FoliageMesh = new Mesh {indexFormat = IndexFormat.UInt32};
            m_MeshCollider = GetComponent<MeshCollider>();
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
            m_Voxels = new Voxel[m_ChunkSize, m_ChunkSize, m_ChunkSize];
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
            if (m_MeshCollider.sharedMesh) m_MeshCollider.sharedMesh.Clear();
        }

        private bool InsideChunk(in Position3Int pos) => pos.x < m_ChunkSize && pos.y < m_ChunkSize && pos.z < m_ChunkSize
                                                      && pos.x >= 0 && pos.y >= 0 && pos.z >= 0;

        public void SetVoxelDataNoCheck(in Position3Int pos, VoxelChangeData changeData) => m_Voxels?[pos.x, pos.y, pos.z].SetVoxelData(changeData);

        public Voxel? GetVoxel(in Position3Int internalPosition)
        {
            return InsideChunk(internalPosition)
                ? GetVoxelNoCheck(internalPosition)
                : m_ChunkManager.GetVoxel(internalPosition + m_Position * m_ChunkSize);
        }

        public Voxel GetVoxelNoCheck(in Position3Int position) { return m_Voxels[position.x, position.y, position.z]; }

        private VoxelChangeData GetChangeDataFromSave(in Position3Int position, MapContainer save)
        {
            float
                noiseHeight = Noise.Simplex(m_Position.x * m_ChunkSize + position.x,
                                            m_Position.z * m_ChunkSize + position.z, save.noise),
                height = noiseHeight + save.terrainHeight - position.y - m_Position.y * m_ChunkSize,
                floatDensity = Mathf.Clamp(height, 0.0f, 2.0f);
            var density = (byte) (floatDensity * byte.MaxValue / 2.0f);
            return new VoxelChangeData
            {
                texture = height > 5.0f ? VoxelId.Stone : VoxelId.Grass,
                renderType = VoxelRenderType.Smooth, density = density, breakable = true, orientation = Orientation.None, natural = true
            };
        }

        public void CreateTerrainFromSave(MapContainer save)
        {
            m_Generating = true;
//            foreach (BrushStroke stroke in save.BrushStrokes)
//            {
//                Position3Int center = stroke.center;
//                byte radius = stroke.radius;
//                if
//                (
//                       center.x - radius > m_Position.x && center.x + radius < m_Position.x
//                    || center.y - radius > m_Position.y && center.y + radius < m_Position.y
//                    || center.z - radius > m_Position.z && center.z + radius < m_Position.z
//                )
//                {
//
//                }    
//            }
            for (var x = 0; x < m_ChunkSize; x++)
            {
                for (var z = 0; z < m_ChunkSize; z++)
                {
                    for (var y = 0; y < m_ChunkSize; y++)
                    {
                        var position = new Position3Int(x, y, z);
                        VoxelChangeData changeData = GetChangeDataFromSave(position, save);
                        SetVoxelDataNoCheck(position, changeData);
                    }
                }
            }
            m_Generating = false;
        }

        public VoxelChangeData RevertToMapSave(in Position3Int position, MapContainer save)
        {
            VoxelChangeData changeData = GetChangeDataFromSave(position, save);
            SetVoxelDataNoCheck(position, changeData);
            return changeData;
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
            m_Updating = true;
            m_SolidMeshData.Clear();
            m_FoliageMeshData.Clear();
            VoxelRenderer.RenderVoxels(m_ChunkManager, this, m_SolidMeshData, m_FoliageMeshData);
            m_Updating = false;
        }

        private void ApplyMesh()
        {
            ApplyMesh(m_SolidMesh, m_SolidMeshData);
            ApplyMesh(m_FoliageMesh, m_FoliageMeshData);
            m_MeshCollider.sharedMesh = m_SolidMesh;
        }

        private static void ApplyMesh(Mesh mesh, MeshData data)
        {
            mesh.Clear();
            mesh.SetVertices(data.vertices);
            mesh.SetIndices(data.triangleIndices.ToArray(), MeshTopology.Triangles, 0);
            mesh.SetUVs(0, data.uvs);
            if (data.normals.Count == 0)
                mesh.RecalculateNormals();
            else
                mesh.SetNormals(data.normals);
            mesh.RecalculateTangents();
        }
    }
}