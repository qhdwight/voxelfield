using Swihoni.Components;
using UnityEngine;

namespace Voxel.Map
{
    public class ModelBehavior : MonoBehaviour
    {
        [SerializeField] private ushort m_Id = default;

        public Container Container { get; private set; }

        public ushort Id => m_Id;

        public void Setup(MapManager mapManager) { }

        public void SetContainer(Container container) => Container = container;
    }
}