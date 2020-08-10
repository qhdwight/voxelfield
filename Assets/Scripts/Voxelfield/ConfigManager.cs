using System;
using Swihoni.Components;
using Swihoni.Sessions.Config;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield
{ 
    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class ConfigManager : ConfigManagerBase
    {
        private static readonly Lazy<PostProcessVolume> Volume = new Lazy<PostProcessVolume>(FindObjectOfType<PostProcessVolume>);

        [Config(ConfigType.Session)] public VoxelMapNameProperty mapName = new VoxelMapNameProperty("Fort");

        [Config] public BoolProperty enableMiniMap = new BoolProperty();
        [Config] public BoolProperty authenticateSteam = new BoolProperty();

        public SecureAreaConfig secureAreaConfig = new SecureAreaConfig();

        public new static ConfigManager Active => (ConfigManager) ConfigManagerBase.Active;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            Debug.Log($"[CPU] {SystemInfo.processorType}: {SystemInfo.processorCount} cores @{SystemInfo.processorFrequency} MHz");
            Debug.Log($"[GPU] {SystemInfo.graphicsDeviceName}: {SystemInfo.graphicsMemorySize} mb");
        }

        protected override void SetQualityLevel(int level)
        {
            base.SetQualityLevel(level);
            Volume.Value.enabled = level > 0;
        }
    }
}