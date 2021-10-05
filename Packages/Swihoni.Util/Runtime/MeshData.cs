using System.Collections.Generic;
using UnityEngine;

namespace Swihoni.Util
{
    public class MeshData
    {
        public readonly List<Vector3> vertices = new(1 << 8);
        public readonly List<int> triangleIndices = new(1 << 8);
        public readonly List<Vector2> uvs = new(1 << 8);
        public readonly List<Vector3> normals = new(1 << 8);
        public readonly List<Color32> colors = new(1 << 8);

        public void Clear()
        {
            vertices.Clear();
            triangleIndices.Clear();
            uvs.Clear();
            normals.Clear();
            colors.Clear();
        }
    }
}