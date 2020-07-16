using Swihoni.Util.Interface;
using UnityEngine;

namespace Voxelfield.Interface
{
    public class HelpInterface : InterfaceBehaviorBase
    {
        private void Update()
        {
            SetInterfaceActive(UnityEngine.Input.GetKey(KeyCode.U));
        }
    }
}