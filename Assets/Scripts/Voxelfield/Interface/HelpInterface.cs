using Swihoni.Sessions;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Voxelfield.Interface
{
    public class HelpInterface : InterfaceBehaviorBase
    {
        private void Update() => SetInterfaceActive(!SessionBase.InterruptingInterface && Input.GetKey(KeyCode.U));
    }
}