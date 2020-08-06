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
        [Config(ConfigType.ServerSession)] public VoxelMapNameProperty mapName = new VoxelMapNameProperty("Fort");

        [Config] public BoolProperty enableMiniMap = new BoolProperty();
        [Config] public BoolProperty authenticateSteam = new BoolProperty();

        public SecureAreaConfig secureAreaConfig = new SecureAreaConfig();
        
        public new static ConfigManager Active => (ConfigManager) ConfigManagerBase.Active;
    }
}