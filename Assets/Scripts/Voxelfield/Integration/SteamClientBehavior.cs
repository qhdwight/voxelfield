#if UNITY_EDITOR
// #define VOXELFIELD_RELEASE_CLIENT
#endif

using System;
using Steamworks;
using Swihoni.Sessions.Config;
using Swihoni.Util;
using UnityEngine;
using UnityEngine.Events;

namespace Voxelfield.Integration
{
    [DisallowMultipleComponent]
    public class SteamClientBehavior : SingletonBehavior<SteamClientBehavior>
    {
        public static UnityEvent steamInitialized;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize() => steamInitialized = new UnityEvent();

#if VOXELFIELD_RELEASE_CLIENT
        private void Start() => TryInitialize();
#endif

        public static void InitializeOrThrow()
        {
            SteamClient.Init(480, false);
            if (SteamClient.IsValid)
            {
                // Dispatch.OnDebugCallback += (type, message, isServer) => Debug.Log($"[Steam Debug] [{type}] [{(isServer ? "Server" : "Client")}] {message}");
                steamInitialized.Invoke();
                steamInitialized.RemoveAllListeners();
            }
        }

        public static void RunOrWait(UnityAction listener)
        {
            if (SteamClient.IsValid) listener();
            else steamInitialized.AddListener(listener);
        }

        protected override void Awake()
        {
            base.Awake();
            ConsoleCommandExecutor.SetCommand("steam_status", arguments =>
            {
                if (!SteamClient.IsValid) TryInitialize();
                if (SteamClient.IsValid) Debug.Log($"Logged in as: {SteamClient.Name} with ID: {SteamClient.SteamId}");
                else Debug.LogWarning("Not connected to steam");
            });
        }

        public static bool TryInitialize()
        {
            try
            {
                InitializeOrThrow();
                return true;
            }
            catch (Exception)
            {
                return false;
            }

            #region Testing

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

            #endregion
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