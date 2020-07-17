using Swihoni.Components;
using Swihoni.Sessions.Config;
using UnityEngine;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield
{
    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class ConfigManager : ConfigManagerBase
    {
        [Config("map_name", ConfigType.ServerSession)] public VoxelMapNameProperty mapName = new VoxelMapNameProperty("Castle");
        [Config("force_start", ConfigType.Server)] public BoolProperty forceStart = new BoolProperty();
        [Config("restart_mode", ConfigType.Server)] public BoolProperty restartMode = new BoolProperty();
        
        [Config("enable_mini_map")] public BoolProperty enableMiniMap = new BoolProperty();

        public SecureAreaConfig secureAreaConfig = new SecureAreaConfig();
    }
}