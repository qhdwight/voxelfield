using UnityEngine;

namespace Voxelfield.Session.Mode
{
    public class CurePackageVisuals : MonoBehaviour
    {
        public void Render(CurePackageComponent cure)
        {
            bool isActive = cure.isActive.Else(false);
            gameObject.SetActive(isActive);
            if (isActive) transform.SetPositionAndRotation(cure.position, Quaternion.identity);
        }
    }
}