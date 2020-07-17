using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;

#endif

namespace Swihoni.Sessions.Config
{
    public enum ConfigType
    {
        Client,
        ServerSession,
        Server
    }

    public class ConfigAttribute : Attribute
    {
        public string Name { get; }
        public ConfigType Type { get; }

        public ConfigAttribute(string name, ConfigType type = ConfigType.Client)
        {
            Name = name;
            Type = type;
        }
    }

    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class ConfigManagerBase : ScriptableObject
    {
        private const string Separator = ":";

        public static ConfigManagerBase Active { get; private set; }

        private static ConfigManagerBase _default;

        private Dictionary<Type, (PropertyBase, ConfigAttribute)> m_TypeToConfig;
        private Dictionary<string, (PropertyBase, ConfigAttribute)> m_NameToConfig;

        [Config("tick_rate", ConfigType.ServerSession)] public TickRateProperty tickRate = new TickRateProperty(60);
        [Config("allow_cheats", ConfigType.ServerSession)] public AllowCheatsProperty allowCheats = new AllowCheatsProperty();
        [Config("mode_id", ConfigType.ServerSession)] public ModeIdProperty modeId = new ModeIdProperty();
        [Config("respawn_duration", ConfigType.Server)] public TimeUsProperty respawnDuration = new TimeUsProperty();
        [Config("respawn_health", ConfigType.Server)] public ByteProperty respawnHealth = new ByteProperty(100);

        [Config("fov")] public ByteProperty fov = new ByteProperty(60);
        [Config("target_fps")] public UShortProperty targetFps = new UShortProperty(200);
        [Config("volume")] public FloatProperty volume = new FloatProperty(0.5f);
        [Config("sensitivity")] public FloatProperty sensitivity = new FloatProperty(2.0f);
        [Config("ads_multiplier")] public FloatProperty adsMultiplier = new FloatProperty(1.0f);
        [Config("crosshair_thickness")] public FloatProperty crosshairThickness = new FloatProperty(1.0f);
        [Config("input")] public InputBindingProperty input = new InputBindingProperty();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            LoadDefaultAsset();

            Active = ((ConfigManagerBase) CreateInstance(_default.GetType())).Introspect();
            ReadActive();

            ConsoleCommandExecutor.SetCommand("restore_default_config", args =>
            {
                WriteDefaults();
                ReadActive();
            });
            ConsoleCommandExecutor.SetCommand("write_config", args => WriteActive());
            ConsoleCommandExecutor.SetCommand("read_config", args => ReadActive());
        }

        private static void LoadDefaultAsset()
        {
            _default = Resources.Load<ConfigManagerBase>("Config").Introspect();
            if (!_default) throw new Exception("No config asset was found in resources");
        }

        private ConfigManagerBase Introspect()
        {
            m_NameToConfig = new Dictionary<string, (PropertyBase, ConfigAttribute)>();
            m_TypeToConfig = new Dictionary<Type, (PropertyBase, ConfigAttribute)>();

            var names = new Stack<string>();
            void Recurse(ElementBase element)
            {
                bool isConfig = element.TryAttribute(out ConfigAttribute config);
                if (isConfig)
                {
                    switch (element)
                    {
                        case ComponentBase component:
                            names.Push(config.Name);
                            foreach (ElementBase childElement in component)
                                Recurse(childElement);
                            names.Pop();
                            break;
                        case PropertyBase property:
                            string fullName = string.Join(".", names.Append(config.Name));
                            m_NameToConfig.Add(fullName, (property, config));
                            if (config.Type == ConfigType.Client)
                            {
                                ConsoleCommandExecutor.SetCommand(fullName, HandleArgs);
                            }
                            else
                            {
                                if (config.Type == ConfigType.ServerSession)
                                    m_TypeToConfig.Add(property.GetType(), (property, config));
                                ConsoleCommandExecutor.SetCommand(fullName, SessionBase.IssueCommand);
                            }
                            break;
                    }
                }
            }
            foreach (FieldInfo field in GetType().GetFields(BindingFlags.Instance | BindingFlags.Public))
            {
                var element = (ElementBase) field.GetValue(this);
                element.Field = field;
                Recurse(element);
            }
            return this;
        }

        public static void HandleArgs(IReadOnlyList<string> split)
        {
            if (Active.m_NameToConfig.TryGetValue(split[0], out (PropertyBase, ConfigAttribute) tuple))
            {
                switch (split.Count)
                {
                    case 2:
                        tuple.Item1.TryParseValue(split[1]);
                        Debug.Log($"Set {split[0]} to {split[1]}");
                        if (tuple.Item2.Type == ConfigType.Client) WriteActive();
                        break;
                    case 1 when tuple.Item1 is BoolProperty boolProperty:
                        boolProperty.Value = true;
                        Debug.Log($"Set {split[0]}");
                        if (tuple.Item2.Type == ConfigType.Client) WriteActive();
                        break;
                }
            }
        }

        public static void UpdateSessionConfig(ComponentBase session)
        {
            foreach (ElementBase element in session.Elements)
                if (element is PropertyBase property && Active.m_TypeToConfig.TryGetValue(property.GetType(), out (PropertyBase, ConfigAttribute) tuple))
                    property.SetFromIfWith(tuple.Item1);
        }

        private static string GetConfigFile()
        {
            string parentFolder = Directory.GetParent(Application.dataPath).FullName;
            if (Application.platform == RuntimePlatform.OSXPlayer) parentFolder = Directory.GetParent(parentFolder).FullName;
            return Path.ChangeExtension(Path.Combine(parentFolder, "Config"), "vfc");
        }

        public static void WriteDefaults() => Write(_default);

        private static void WriteActive() => Write(Active);

        private static void Write(ConfigManagerBase config)
        {
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, (PropertyBase, ConfigAttribute)> pair in config.m_NameToConfig)
            {
                builder.Append(pair.Key).Append(Separator).Append(" ").AppendPropertyValue(pair.Value.Item1).Append("\n");
            }
            string configPath = GetConfigFile();
            File.WriteAllText(configPath, builder.ToString());
            Debug.Log($"Wrote config to {configPath}");
        }

        private static void ReadActive()
        {
            string configPath = GetConfigFile();
            if (!File.Exists(configPath)) WriteDefaults();
            string[] lines = File.ReadAllLines(configPath);
            foreach (string line in lines)
            {
                string[] cells = line.Split(new[] {Separator}, StringSplitOptions.RemoveEmptyEntries);
                string key = cells[0], stringValue = cells[1].Trim();
                Active.m_NameToConfig[key].Item1.TryParseValue(stringValue);
            }
            Debug.Log($"Read config from {configPath}");
        }
    }
}