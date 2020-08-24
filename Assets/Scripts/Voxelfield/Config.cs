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
    public class Config : DefaultConfig
    {
        private static Lazy<PostProcessVolume> _volume;

        [Config(ConfigType.Session)] public VoxelMapNameProperty mapName = new VoxelMapNameProperty("Castle");

        [Config] public BoolProperty enableMiniMap = new BoolProperty();
        [Config] public BoolProperty authenticateSteam = new BoolProperty();

        public SecureAreaConfig secureAreaConfig = new SecureAreaConfig();

        public new static Config Active => (Config) DefaultConfig.Active;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void PreInitialize() => _volume = new Lazy<PostProcessVolume>(FindObjectOfType<PostProcessVolume>);

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            Debug.Log($"[CPU] {SystemInfo.processorType}: {SystemInfo.processorCount} cores @{SystemInfo.processorFrequency} MHz");
            Debug.Log($"[GPU] {SystemInfo.graphicsDeviceName}: {SystemInfo.graphicsMemorySize} mb");
        }

        protected override void SetQualityLevel(int level)
        {
            base.SetQualityLevel(level);
            _volume.Value.enabled = level > 0;
        }
    }
}