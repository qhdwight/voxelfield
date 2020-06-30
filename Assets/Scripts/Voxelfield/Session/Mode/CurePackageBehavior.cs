using UnityEngine;
using Voxel.Map;

namespace Voxelfield.Session.Mode
{
    public class CurePackageBehavior : ModelBehavior
    {
        [SerializeField] private GameObject m_Cure;
        
        public void Render(CurePackageComponent cure)
        {
            if (cure.isActive.WithoutValue)
            {
                gameObject.SetActive(false);
                return;
            }
            bool isCureActive = cure.isActive;
            gameObject.SetActive(true);
            m_Cure.SetActive(isCureActive);
        }
    }
}