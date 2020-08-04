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
        ServerSession
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

        private static readonly Lazy<ConfigManagerBase> Default = new Lazy<ConfigManagerBase>(LoadDefault);

        private static ConfigManagerBase LoadDefault()
        {
            ConfigManagerBase defaults = Resources.Load<ConfigManagerBase>("Config").Introspect();
#if !UNITY_EDITOR
            if (defaults.resolutionWidth.WithoutValue) defaults.resolutionWidth.Value = Screen.width / 2;
            if (defaults.resolutionHeight.WithoutValue) defaults.resolutionHeight.Value = Screen.height / 2;
            if (defaults.refreshRate.WithoutValue) defaults.refreshRate.Value = Screen.currentResolution.refreshRate;
            if (defaults.fullScreen.WithoutValue) defaults.fullScreen.Value = FullScreenMode.Windowed;
#endif
            defaults.input = new InputBindingProperty();
            return defaults;
        }

        private Dictionary<Type, (PropertyBase, ConfigAttribute)> m_TypeToConfig;
        private Dictionary<string, (PropertyBase, ConfigAttribute)> m_NameToConfig;

        [Config("tick_rate", ConfigType.ServerSession)] public TickRateProperty tickRate = new TickRateProperty(60);
        [Config("allow_cheats", ConfigType.ServerSession)] public AllowCheatsProperty allowCheats = new AllowCheatsProperty();
        [Config("mode_id", ConfigType.ServerSession)] public ModeIdProperty modeId = new ModeIdProperty();

        [Config("respawn_duration", ConfigType.ServerSession)] public TimeUsProperty respawnDuration = new TimeUsProperty();
        [Config("respawn_health", ConfigType.ServerSession)] public ByteProperty respawnHealth = new ByteProperty(100);

        [Config("fov")] public ByteProperty fov = new ByteProperty(60);
        [Config("target_fps")] public UShortProperty targetFps = new UShortProperty(200);
        [Config("volume")] public FloatProperty volume = new FloatProperty(0.5f);
        [Config("sensitivity")] public FloatProperty sensitivity = new FloatProperty(2.0f);
        [Config("ads_multiplier")] public FloatProperty adsMultiplier = new FloatProperty(1.0f);
        [Config("crosshair_thickness")] public FloatProperty crosshairThickness = new FloatProperty(1.0f);
        [Config("input_bindings")] public InputBindingProperty input = new InputBindingProperty();
        [Config("fps_update_rate")] public FloatProperty fpsUpdateRate = new FloatProperty(0.4f);
        [Config("log_prediction_errors")] public BoolProperty logPredictionErrors = new BoolProperty();
        [Config("previous_commands")] public ListProperty<StringProperty> consoleHistory = new ListProperty<StringProperty>(32);

        [DisplayConfig("resolution_width")] public IntProperty resolutionWidth = new IntProperty();
        [DisplayConfig("resolution_height")] public IntProperty resolutionHeight = new IntProperty();
        [DisplayConfig("refresh_rate")] public IntProperty refreshRate = new IntProperty();
        [DisplayConfig("fullscreen_mode")] public BoxedEnumProperty<FullScreenMode> fullScreen = new BoxedEnumProperty<FullScreenMode>();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            Active = ((ConfigManagerBase) CreateInstance(Default.Value.GetType())).Introspect();
// #if UNITY_EDITOR
//             SetActiveToDefault();
// #else
//             ReadActive();
// #endif
            ReadActive();

            ConsoleCommandExecutor.SetCommand("restore_default_config", arguments =>
            {
                WriteDefaults();
                SetActiveToDefault();
            });
            ConsoleCommandExecutor.SetCommand("write_config", arguments => WriteActive());
            ConsoleCommandExecutor.SetCommand("read_config", arguments => ReadActive());
            ConsoleCommandExecutor.SetCommand("open_config", arguments => Application.OpenURL($"file://{GetConfigFile()}"));
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
                                ConsoleCommandExecutor.SetCommand(fullName, TryHandleArguments);
                            }
                            else
                            {
                                if (config.Type == ConfigType.ServerSession)
                                    m_TypeToConfig.Add(property.GetType(), (property, config));
                                ConsoleCommandExecutor.SetCommand(fullName, SessionBase.IssueSessionCommand);
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

        public static void TryHandleArguments(IReadOnlyList<string> split)
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
                            property.TryParse(split[1]);
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
            WriteActive();
            config.Update();
        }

        public static void UpdateSessionConfig(ComponentBase session)
        {
            // ReSharper disable once ForCanBeConvertedToForeach - Avoid allocation of getting enumerator
            for (var i = 0; i < session.Count; i++)
            {
                if (session[i] is PropertyBase property && Active.m_TypeToConfig.TryGetValue(property.GetType(), out (PropertyBase, ConfigAttribute) tuple))
                    property.SetFromIfWith(tuple.Item1);
            }
        }

        public static string GetConfigFile()
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
                        string key = cells[0], stringValue = cells.Length == 1 ? string.Empty : cells[1].Trim();
                        PropertyBase property = Active.m_NameToConfig[key].Item1;
                        if (stringValue == "None") property.Clear();
                        else property.ParseValue(stringValue);
                    }
                    OnActiveLoaded();
                }
                else
                {
                    WriteDefaults();
                    Debug.LogWarning($"[{Default.GetType().Name}] Config file was not found so a default one was written");
                    SetActiveToDefault();
                }
#if UNITY_EDITOR
                Debug.Log($"Read config from {configPath}");
#endif
            }
            catch (Exception exception)
            {
                SetActiveToDefault();
                Debug.LogError($"[{Default.GetType().Name}] Failed to parse config. Using defaults. Check permissions? {exception.Message}");
                if (Debug.isDebugBuild) Debug.LogError(exception);
            }
        }

        private static void OnActiveLoaded()
        {
            foreach (StringProperty command in Active.consoleHistory.List)
                ConsoleCommandExecutor.InsertPreviousCommand(command.AsNewString());
        }

        private static void SetActiveToDefault()
        {
            foreach (KeyValuePair<string, (PropertyBase, ConfigAttribute)> pair in Default.Value.m_NameToConfig)
                Active.m_NameToConfig[pair.Key].Item1.SetTo(pair.Value.Item1);
        }

        public static void OnCommand(string command)
        {
            Active.consoleHistory.Add(new StringProperty(command));
            WriteActive();
        }
    }
}