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
#if VOXELFIELD_RELEASE_CLIENT
        private void Start() => TryInitialize();
#endif

        public static void InitializeOrThrow() => SteamClient.Init(480, false);

        public static void TryInitialize()
        {
            try
            {
                InitializeOrThrow();
            }
            catch (Exception)
            {
                // ignored
            }
            // try
            // {
            //     if (!SteamServer.IsValid)
            //     {
            //         var parameters = new SteamServerInit
            //         {
            //             DedicatedServer = true,
            //             GamePort = 27015, QueryPort = 27016,
            //             Secure = true,    
            //             VersionString = Application.version,
            //             GameDescription = Application.productName,
            //             IpAddress = NetUtils.ResolveAddress(NetUtils.GetLocalIp(LocalAddrType.IPv4)),
            //             ModDir = Application.productName,
            //             SteamPort = 0
            //         };
            //         SteamServer.Init(480, parameters, false);
            //         Debug.Log("Init **********************");
            //         SteamServer.LogOnAnonymous();
            //         Debug.Log("Logon **********************");
            //     }
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.Log("Successfully initialized Steam");
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.Log("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     
            //     SteamServer.Shutdown();
            // }
            // catch (Exception exception)
            // {
            //     Debug.LogError("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.LogError("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.LogError($"Failed to initialize Steam {exception.Message}");
            //     Debug.LogError("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            //     Debug.LogError("@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@@");
            // }
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