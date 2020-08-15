using System;
using System.Threading.Tasks;
using Discord;
using Swihoni.Sessions;
using Swihoni.Sessions.Config;
using Swihoni.Util;
using UnityEngine;
using DiscordClient = Discord.Discord;

namespace Voxelfield.Integration
{
    public delegate void ModifyActivity(ref Activity activity);

    public class DiscordManager : SingletonBehavior<DiscordManager>
    {
        public const long ClientId = 742661586484854824;

        public static DiscordClient Client { get; private set; }
        public static ActivityManager ActivityManager => Client?.GetActivityManager();

        public static void SetActivity(ModifyActivity modify)
        {
            if (ActivityManager == null) return;
            var activity = new Activity
            {
                Type = ActivityType.Playing,
                Timestamps = new ActivityTimestamps {Start = SessionExtensions.UnixNow},
                Assets = new ActivityAssets {LargeImage = "logo"}
            };
            modify(ref activity);
            ActivityManager.UpdateActivity(activity, result =>
            {
// #if !VOXELFIELD_RELEASE_CLIENT
//                 Debug.Log($"[Discord] Setting status to: {activity.State} result: {result}");
// #endif
            });
        }

        public static void SetActivity(string state) => SetActivity((ref Activity activity) => activity.State = state);

        protected override void Awake()
        {
            base.Awake();
            ConsoleCommandExecutor.SetCommand("discord_status", async args =>
            {
                if (Client == null) Debug.LogWarning("Not connected to Discord");
                else
                {
                    UserManager manager = Client.GetUserManager();
                    await Task.Delay(250);
                    User currentUser = manager.GetCurrentUser();
                    Debug.Log($"Logged in as: {currentUser.Username}#{currentUser.Discriminator} with ID: {currentUser.Id}");
                }
            });
            Client = new DiscordClient(ClientId, (ulong) CreateFlags.NoRequireDiscord);
            Client?.SetLogHook(LogLevel.Debug, OnLog);
        }

        private static void OnLog(LogLevel level, string message)
        {
            string logMessage = $"[Discord Hook] {message}";
            switch (level)
            {
                case LogLevel.Error:
                    Debug.LogError(logMessage);
                    break;
                case LogLevel.Warn:
                    Debug.LogWarning(logMessage);
                    break;
                case LogLevel.Info:
                case LogLevel.Debug:
                    Debug.Log(logMessage);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(level), level, null);
            }
        }

        private void Update() => Client?.RunCallbacks();

        private void OnApplicationQuit()
        {
            ActivityManager?.ClearActivity(result => { });
            Client?.Dispose();
        }
    }
}