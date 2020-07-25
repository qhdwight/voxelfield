// #undef UNITY_EDITOR

using UnityEngine;
#if !UNITY_EDITOR
using System;
using Steamworks;
#endif

namespace Swihoni.Util
{
    [DisallowMultipleComponent]
    public class SteamClientBehavior : MonoBehaviour
    {
#if !UNITY_EDITOR
        private void Awake()
        {
            try
            {
                SteamClient.Init(480, false);
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
#endif
    }
}