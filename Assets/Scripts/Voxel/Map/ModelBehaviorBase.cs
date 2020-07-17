using Swihoni.Components;
using Swihoni.Util.Math;
using UnityEngine;

namespace Voxel.Map
{
    public abstract class ModelBehaviorBase : MonoBehaviour
    {
        [SerializeField] private ushort m_Id = default;
        [SerializeField] private string m_ModelName = default;

        public Position3Int Position { get; private set; }
        public Container Container { get; private set; }

        public ushort Id => m_Id;
        public string ModelName => m_ModelName;

        public void Setup(MapManager mapManager) { }

        public void Set(in Position3Int position, Container container)
        {
            Position = position;
            Container = container;
        }

        public virtual void RenderContainer() { }

        public abstract void SetInMode(Container session);
    }
}