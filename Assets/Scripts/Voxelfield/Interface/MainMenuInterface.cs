using System.Threading.Tasks;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Interfaces;
using UnityEngine;
using UnityEngine.UI;
using Voxelfield.Session;

namespace Voxelfield.Interface
{
    public class MainMenuInterface : SessionInterfaceBehavior
    {
        private const string InMainMenu = "In Main Menu", Searching = "Searching for a Game";

        public override void Render(in SessionContext context) { }

        public async void OnPlayButton(Button button)
        {
            GameObject progress = button.GetComponentInChildren<Animator>(true).gameObject;
            progress.SetActive(true);
            button.interactable = false;
            await Task.Delay(500);
            if (progress) progress.SetActive(false);
            if (button) button.interactable = true;
            SessionManager.StartHost();
        }

        public void OnSettingsButton() => DefaultConfig.OpenSettings();

        public void OnQuitButton() => Application.Quit();
    }
}