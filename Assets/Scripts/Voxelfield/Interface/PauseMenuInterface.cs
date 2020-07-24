using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield.Interface
{
    public class PauseMenuInterface : SessionInterfaceBehavior
    {
        private bool m_PlayerWantsVisible;

        public override void Render(SessionBase session, Container sessionContainer)
        {
            if (InputProvider.GetInputDown(InputType.PauseMenu)) m_PlayerWantsVisible = !m_PlayerWantsVisible;
            
            SetInterfaceActive(m_PlayerWantsVisible);
        }

        public void ConfigurationButton() { }

        public void QuitButton() => Application.Quit();

        public void DisconnectButton() => SessionManager.DisconnectAll();
    }
}