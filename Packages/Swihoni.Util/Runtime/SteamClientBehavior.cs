#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_CLIENT
#endif

using System;
using Steamworks;
using UnityEngine;

namespace Swihoni.Util
{
    [DisallowMultipleComponent]
    public class SteamClientBehavior : SingletonBehavior<SteamClientBehavior>
    {
        protected override void Awake()
        {
            base.Awake();
            Initialize();
        }

        public static void InitializeOrThrow() => SteamClient.Init(480, false);
        
        public static void Initialize()
        {
            try
            {
                InitializeOrThrow();
                Debug.Log("Successfully initialized Steam client");
            }
            catch (Exception)
            {
                Debug.LogWarning("Failed to initialize Steam client");
            }
        }

        private void Update()
        {
            if (SteamClient.IsValid) SteamClient.RunCallbacks();
        }

        private void OnApplicationQuit()
        {
            if (SteamClient.IsValid) SteamClient.Shutdown();
        }
    }
}