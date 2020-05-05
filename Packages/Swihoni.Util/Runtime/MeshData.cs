using System.Collections.Generic;
using UnityEngine;

namespace Swihoni.Util
{
    public class MeshData
    {
        public readonly List<Vector3> vertices = new List<Vector3>();
        public readonly List<int> triangleIndices = new List<int>();
        public readonly List<Vector2> uvs = new List<Vector2>();
        public readonly List<Vector3> normals = new List<Vector3>();

        public void Clear()
        {
            vertices.Clear();
            triangleIndices.Clear();
            uvs.Clear();
            normals.Clear();
        }
    }
}