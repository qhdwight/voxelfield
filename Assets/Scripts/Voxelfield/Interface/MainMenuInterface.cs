using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Voxelfield.Integration;

namespace Voxelfield.Interface
{
    public class MainMenuInterface : SessionInterfaceBehavior
    {
        private const string InMainMenu = "In Main Menu", Searching = "Searching for a Game";

        public override void Render(in SessionContext context) { }

        private void Update() => SetInterfaceActive(SessionBase.SessionCount == 0);

        public override void SetInterfaceActive(bool isActive)
        {
            if (IsActive == isActive) return;
            if (isActive) DiscordManager.SetActivity(InMainMenu);

            base.SetInterfaceActive(isActive);
        }

        public async void OnPlayButton(Button button)
        {
            GameObject progress = button.GetComponentInChildren<Animator>(true).gameObject;
            progress.SetActive(true);
            button.interactable = false;
            try
            {
                DiscordManager.SetActivity(Searching);
                await GameLiftClientManager.QuickPlayAsync();
            }
            finally
            {
                if (button) button.interactable = true;
                if (progress) progress.SetActive(false);
                DiscordManager.SetActivity(InMainMenu);
            }
        }

        public void OnSettingsButton() => DefaultConfig.OpenSettings();

        public void OnQuitButton() => Application.Quit();
    }
}