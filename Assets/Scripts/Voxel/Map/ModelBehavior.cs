using Swihoni.Components;
using Swihoni.Util.Math;
using UnityEngine;

namespace Voxel.Map
{
    public class ModelBehavior : MonoBehaviour
    {
        [SerializeField] private ushort m_Id = default;

        public Position3Int Position { get; private set; }
        public Container Container { get; private set; }

        public ushort Id => m_Id;

        public void Setup(MapManager mapManager) { }

        public void Set(in Position3Int position, Container container)
        {
            Position = position;
            Container = container;
        }
    }
}