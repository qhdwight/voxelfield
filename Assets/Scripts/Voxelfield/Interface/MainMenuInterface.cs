using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using UnityEngine;
using UnityEngine.UI;

namespace Voxelfield.Interface
{
    public class MainMenuInterface : SessionInterfaceBehavior
    {
        public override void Render(in SessionContext context) { }

        private void Update() => SetInterfaceActive(SessionBase.SessionCount == 0);

        public async void OnPlayButton(Button button)
        {
            button.interactable = false;
            try
            {
                await GameLiftClientManager.QuickPlayAsync();
            }
            finally
            {
                button.interactable = true;
            }
        }

        public void OnSettingsButton() => ConfigManagerBase.OpenSettings();

        public void OnQuitButton() => Application.Quit();
    }
}