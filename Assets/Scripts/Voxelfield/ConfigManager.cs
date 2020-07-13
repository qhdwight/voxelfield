using Swihoni.Sessions;
using UnityEngine;
using Voxelfield.Session;

namespace Voxelfield
{
    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class ConfigManager : ConfigManagerBase
    {
        [Config("map_name", true)] public VoxelMapNameProperty mapName = new VoxelMapNameProperty("Castle");
    }
}