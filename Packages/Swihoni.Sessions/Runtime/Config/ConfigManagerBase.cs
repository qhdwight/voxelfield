using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Swihoni.Sessions.Config
{
    public enum ConfigType
    {
        Client,
        ServerSession,
        Server,
        ServerSingleTick
    }

    public abstract class ConfigAttributeBase : Attribute
    {
        internal virtual void Update() { }
    }

    public class ConfigAttribute : ConfigAttributeBase
    {
        public string Name { get; }
        public ConfigType Type { get; }

        public ConfigAttribute(string name, ConfigType type = ConfigType.Client)
        {
            Name = name;
            Type = type;
        }
    }

    public class DisplayConfigAttribute : ConfigAttribute
    {
#if !UNITY_EDITOR
        internal override void Update() => Screen.SetResolution(ConfigManagerBase.Active.resolutionWidth, ConfigManagerBase.Active.resolutionHeight,
                                                                ConfigManagerBase.Active.fullScreen, ConfigManagerBase.Active.refreshRate);
#endif

        public DisplayConfigAttribute(string name) : base(name) { }
    }

    [Serializable]
    public class FullscreenProperty : BoxedEnumProperty<FullScreenMode>
    {
        public FullscreenProperty() { }
        public FullscreenProperty(FullScreenMode value) : base(value) { }
    }

    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class ConfigManagerBase : ScriptableObject
    {
        private const string Separator = ":";

        public static ConfigManagerBase Active { get; private set; }

        private static readonly Lazy<ConfigManagerBase> Default = new Lazy<ConfigManagerBase>(() => Resources.Load<ConfigManagerBase>("Config").Introspect());

        private Dictionary<Type, (PropertyBase, ConfigAttribute)> m_TypeToConfig;
        private Dictionary<string, (PropertyBase, ConfigAttribute)> m_NameToConfig;

        [Config("tick_rate", ConfigType.ServerSession)] public TickRateProperty tickRate = new TickRateProperty(60);
        [Config("allow_cheats", ConfigType.ServerSession)] public AllowCheatsProperty allowCheats = new AllowCheatsProperty();
        [Config("mode_id", ConfigType.ServerSession)] public ModeIdProperty modeId = new ModeIdProperty();

        [Config("respawn_duration", ConfigType.Server)] public TimeUsProperty respawnDuration = new TimeUsProperty();
        [Config("respawn_health", ConfigType.Server)] public ByteProperty respawnHealth = new ByteProperty(100);

        [Config("restart_mode", ConfigType.ServerSingleTick)] public BoolProperty restartMode = new BoolProperty();

        [Config("fov")] public ByteProperty fov = new ByteProperty(60);
        [Config("target_fps")] public UShortProperty targetFps = new UShortProperty(200);
        [Config("volume")] public FloatProperty volume = new FloatProperty(0.5f);
        [Config("sensitivity")] public FloatProperty sensitivity = new FloatProperty(2.0f);
        [Config("ads_multiplier")] public FloatProperty adsMultiplier = new FloatProperty(1.0f);
        [Config("crosshair_thickness")] public FloatProperty crosshairThickness = new FloatProperty(1.0f);
        [Config("input_bindings")] public InputBindingProperty input = new InputBindingProperty();
        [Config("fps_update_rate")] public FloatProperty fpsUpdateRate = new FloatProperty(0.4f);

        [DisplayConfig("resolution_width")] public IntProperty resolutionWidth = new IntProperty();
        [DisplayConfig("resolution_height")] public IntProperty resolutionHeight = new IntProperty();
        [DisplayConfig("refresh_rate")] public IntProperty refreshRate = new IntProperty();
        [DisplayConfig("fullscreen_mode")] public BoxedEnumProperty<FullScreenMode> fullScreen = new BoxedEnumProperty<FullScreenMode>(FullScreenMode.Windowed);

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Active = ((ConfigManagerBase) CreateInstance(Default.Value.GetType())).Introspect();
#if UNITY_EDITOR
            SetActiveToDefault();
#else
            ReadActive();
#endif

            ConsoleCommandExecutor.SetCommand("restore_default_config", args =>
            {
                WriteDefaults();
                SetActiveToDefault();
            });
            ConsoleCommandExecutor.SetCommand("write_config", args => WriteActive());
            ConsoleCommandExecutor.SetCommand("read_config", args => ReadActive());
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
                (PropertyBase property, ConfigAttribute attribute) = tuple;
                switch (split.Count)
                {
                    case 2:
                        if (split[1] == "None")
                        {
                            property.Clear();
                            Debug.Log($"Cleared {split[0]}");
                        }
                        else
                        {
                            property.TryParseValue(split[1]);
                            Debug.Log($"Set {split[0]} to {split[1]}");
                        }
                        OnConfigUpdated(property, attribute);
                        break;
                    case 1 when property is BoolProperty boolProperty:
                        boolProperty.Value = true;
                        Debug.Log($"Set {split[0]}");
                        OnConfigUpdated(property, attribute);
                        break;
                }
            }
        }

        public static void OnConfigUpdated(PropertyBase property, ConfigAttribute config)
        {
            if (config.Type == ConfigType.Client) WriteActive();
            config.Update();
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

        public static void WriteDefaults() => Write(Default.Value);

        private static void WriteActive() => Write(Active);

        private static void Write(ConfigManagerBase config)
        {
            var builder = new StringBuilder();
            foreach (KeyValuePair<string, (PropertyBase, ConfigAttribute)> pair in config.m_NameToConfig)
            {
                builder.Append(pair.Key).Append(Separator).Append(" ").AppendProperty(pair.Value.Item1).Append("\n");
            }
            string configPath = GetConfigFile();
            File.WriteAllText(configPath, builder.ToString());
#if UNITY_EDITOR
            Debug.Log($"Wrote config to {configPath}");
#endif
        }

        private static void ReadActive()
        {
            try
            {
                string configPath = GetConfigFile();
                if (File.Exists(configPath))
                {
                    string[] lines = File.ReadAllLines(configPath);
                    foreach (string line in lines)
                    {
                        string[] cells = line.Split(new[] {Separator}, StringSplitOptions.RemoveEmptyEntries);
                        string key = cells[0], stringValue = cells[1].Trim();
                        PropertyBase property = Active.m_NameToConfig[key].Item1;
                        if (stringValue == "None") property.Clear();
                        else property.TryParseValue(stringValue);
                    }
                }
                else
                {
                    WriteDefaults();
                    Debug.LogWarning("Config file was not found so a default one was written");
                    SetActiveToDefault();
                }
#if UNITY_EDITOR
                Debug.Log($"Read config from {configPath}");
#endif
            }
            catch (Exception exception)
            {
                SetActiveToDefault();
                Debug.LogError($"Failed to write config. Check permissions? {exception.Message}");
            }
        }

        private static void SetActiveToDefault()
        {
            foreach (KeyValuePair<string, (PropertyBase, ConfigAttribute)> pair in Default.Value.m_NameToConfig)
                Active.m_NameToConfig[pair.Key].Item1.SetTo(pair.Value.Item1);
        }
    }
}