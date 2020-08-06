using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using Voxelfield.Session;
#if UNITY_EDITOR
using UnityEditor;

#else
using UnityEngine;
#endif

namespace Voxelfield.Interface
{
    public class PauseMenuInterface : SessionInterfaceBehavior
    {
        public override void Render(in SessionContext context)
        {
            if (NoInterrupting && InputProvider.GetInputDown(InputType.TogglePauseMenu)) ToggleInterfaceActive();
        }

        public void ConfigurationButton() { }

        public void QuitButton() =>
#if UNITY_EDITOR
            EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif

        public void DisconnectButton() => SessionManager.DisconnectAll();
    }
}