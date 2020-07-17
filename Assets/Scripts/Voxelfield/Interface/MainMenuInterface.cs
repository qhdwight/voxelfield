using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Interfaces;

namespace Voxelfield.Interface
{
    public class MainMenuInterface : SessionInterfaceBehavior
    {
        public override void Render(SessionBase session, Container sessionContainer) { }

        private void Update() { SetInterfaceActive(SessionBase.Sessions.Count == 0); }
    }
}