using Swihoni.Sessions;
using Swihoni.Sessions.Interfaces;
using UnityEngine;

namespace Voxelfield.Interface
{
    public class HelpInterface : SessionInterfaceBehavior
    {
        public override void Render(in SessionContext context)
            => SetInterfaceActive(!SessionBase.InterruptingInterface && Input.GetKey(KeyCode.U));
    }
}