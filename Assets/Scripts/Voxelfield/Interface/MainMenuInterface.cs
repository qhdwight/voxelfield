using System;
using System.Threading.Tasks;
using Discord;
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
        public const string InMainMenu = "In Main Menu", Searching = "Searching for a Game";
        
        public override void Render(in SessionContext context) { }

        private void Update() => SetInterfaceActive(SessionBase.SessionCount == 0);
        
        public override void SetInterfaceActive(bool isActive)
        {
            if (IsActive == isActive) return;
            if (isActive) SetStatus(InMainMenu);
            
            base.SetInterfaceActive(isActive);
        }

        private static void SetStatus(string status)
        {
            DiscordManager.ActivityManager?.UpdateActivity(new Activity
            {
                State = status,
                Type = ActivityType.Playing,
                Timestamps = new ActivityTimestamps
                {
                    Start = new DateTimeOffset(DateTime.UtcNow).ToUnixTimeSeconds()
                },
                Assets = new ActivityAssets
                {
                    LargeImage = "logo"
                }
            }, result => Debug.Log($"[Discord] Setting status to: {status} result: {result}"));
        }

        public async void OnPlayButton(Button button)
        {
            GameObject progress = button.GetComponentInChildren<Animator>(true).gameObject;
            progress.SetActive(true);
            button.interactable = false;
            try
            {
                SetStatus(Searching);
                await GameLiftClientManager.QuickPlayAsync();
            }
            finally
            {
                if (button) button.interactable = true;
                if (progress) progress.SetActive(false);
                SetStatus(InMainMenu);
            }
        }

        public void OnSettingsButton() => DefaultConfig.OpenSettings();

        public void OnQuitButton() => Application.Quit();
    }
}