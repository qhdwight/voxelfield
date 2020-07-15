using Swihoni.Components;
using Swihoni.Sessions;
using UnityEngine;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield
{
    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class ConfigManager : ConfigManagerBase
    {
        [Config("map_name", true)] public VoxelMapNameProperty mapName = new VoxelMapNameProperty("Castle");
        [Config("enable_mini_map")] public BoolProperty enableMiniMap = new BoolProperty();
        [Config("force_start")] public BoolProperty forceStart = new BoolProperty();
        [Config("restart_mode")] public BoolProperty restartMode = new BoolProperty();
        public SecureAreaConfig secureAreaConfig = new SecureAreaConfig();
    }
}