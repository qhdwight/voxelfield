using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Util;
using UnityEngine;

namespace Swihoni.Sessions.Config
{
    public enum ConfigType
    {
        Client,
        Session,
        Mode
    }

    public class ConfigAttribute : Attribute
    {
        public string Name { get; set; }
        public ConfigType Type { get; }

        public ConfigAttribute(ConfigType type = ConfigType.Client, string name = null)
        {
            Name = name;
            Type = type;
        }
    }

    [Serializable]
    public class ResolutionProperty : PropertyBase<Resolution>
    {
        private const char Separator = ';';

        public override StringBuilder AppendValue(StringBuilder builder)
            => builder.Append(Value.width).Append(Separator).Append(" ").Append(Value.height).Append(Separator).Append(" ").Append(Value.refreshRate);

        public override void ParseValue(string stringValue)
        {
            string[] split = stringValue.Split(new[] {Separator}, StringSplitOptions.RemoveEmptyEntries);
            Value = new Resolution {width = int.Parse(split[0]), height = int.Parse(split[1]), refreshRate = int.Parse(split[2])};
        }
    }

    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class DefaultConfig : ScriptableObject
    {
        private const string Separator = ":";

        public static DefaultConfig Active { get; private set; }

        private static readonly Lazy<DefaultConfig> Default = new(LoadDefault);
        private static string _logTag;

        private static DefaultConfig LoadDefault()
        {
            DefaultConfig defaults = Resources.Load<DefaultConfig>("Config").Introspect();
            defaults.inputBindings = new InputBindingProperty();
            return defaults;
        }

        private Dictionary<Type, (PropertyBase, ConfigAttribute)> m_TypeToConfig;
        private Dictionary<string, (PropertyBase, ConfigAttribute)> m_NameToConfig;

        [Config(ConfigType.Session)] public TickRateProperty tickRate = new(60);
        [Config(ConfigType.Session)] public AllowCheatsProperty allowCheats = new();
        [Config(ConfigType.Session)] public ModeIdProperty modeId = new();

        [Config(ConfigType.Session)] public TimeUsProperty respawnDuration = new();
        [Config(ConfigType.Session)] public ByteProperty respawnHealth = new(100);

        [Config] public ByteProperty fov = new(60);
        [Config] public UShortProperty targetFps = new(200);
        [Config] public FloatProperty volume = new(0.5f);
        [Config] public FloatProperty sensitivity = new(2.0f);
        [Config] public FloatProperty adsMultiplier = new(1.0f);
        [Config] public FloatProperty crosshairThickness = new(1.0f);
        [Config] public InputBindingProperty inputBindings = new();
        [Config] public BoolProperty invertScrollWheel = new(false);
        [Config] public FloatProperty fpsUpdateRate = new(0.4f);
        [Config] public BoolProperty logPredictionErrors = new();
        [Config] public ListProperty<StringProperty> consoleHistory = new(32);
        [Config] public BoolProperty showDebugInterface = new(true);

        [Config] public IntProperty qualityLevel = new();
        [Config] public ResolutionProperty resolution = new();
        [Config] public BoxedEnumProperty<FullScreenMode> fullScreenMode = new();

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Initialize()
        {
            _logTag = Default.Value.GetType().Name;

            Active = ((DefaultConfig) CreateInstance(Default.Value.GetType())).Introspect();
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

        private DefaultConfig Introspect()
        {
            m_NameToConfig = new Dictionary<string, (PropertyBase, ConfigAttribute)>();
            m_TypeToConfig = new Dictionary<Type, (PropertyBase, ConfigAttribute)>();

            var names = new Stack<string>();
            void Recurse(ElementBase element)
            {
                bool isConfig = element.TryAttribute(out ConfigAttribute config);
                if (isConfig)
                {
                    config.Name ??= element.Field.Name.ToSnakeCase();
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
                                if (config.Type == ConfigType.Session)
                                    m_TypeToConfig.Add(property.GetType(), (property, config));
                                SessionBase.RegisterSessionCommand(fullName);
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

            PostIntrospect();

            return this;
        }

        protected virtual void PostIntrospect()
        {
            if (qualityLevel.WithoutValue)
                qualityLevel.Value = QualitySettings.GetQualityLevel();
        }

        public static void TryHandleArguments(IReadOnlyList<string> split)
        {
            if (Active.m_NameToConfig.TryGetValue(split[0], out (PropertyBase, ConfigAttribute) tuple))
            {
                (PropertyBase property, ConfigAttribute attribute) = tuple;
                switch (split.Count)
                {
                    case 2:
                        if (split[1] == "none")
                        {
                            property.Clear();
                            Debug.Log($"[{_logTag}] Cleared {split[0]}");
                        }
                        else
                        {
                            property.TryParseValue(split[1]);
                            Debug.Log($"[{_logTag}] Set {split[0]} to {split[1]}");
                        }
                        Active.OnConfigUpdated(property, attribute);
                        break;
                    case 1 when property is BoolProperty boolProperty:
                        boolProperty.Set();
                        Debug.Log($"[{_logTag}] Set {split[0]}");
                        Active.OnConfigUpdated(property, attribute);
                        break;
                }
            }
        }

        [SuppressMessage("ReSharper", "PossibleNullReferenceException")]
        protected virtual void OnConfigUpdated(PropertyBase property, ConfigAttribute attribute, bool write = true)
        {
            if (!Application.isBatchMode && !Application.isEditor)
            {
                if (ReferenceEquals(property, resolution) && resolution.TryWithValue(out Resolution r))
                    Screen.SetResolution(r.width, r.height, fullScreenMode.Else(Screen.fullScreenMode), r.refreshRate);
                if (ReferenceEquals(property, fullScreenMode) && fullScreenMode.TryWithValue(out FullScreenMode mode))
                    Screen.fullScreenMode = mode;
            }
            if (ReferenceEquals(property, qualityLevel) && qualityLevel.TryWithValue(out int level))
                SetQualityLevel(level);
            if (write) WriteActive();
        }

        protected virtual void SetQualityLevel(int level) => QualitySettings.SetQualityLevel(level);

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

        public static void WriteDefaults(bool open = false)
        {
            Write(Default.Value);
            if (open) OpenSettings();
        }

        public static void OpenSettings() => Application.OpenURL($"file://{GetConfigFile()}");

        private static void WriteActive() => Write(Active);

        private static void Write(DefaultConfig config)
        {
            var builder = new StringBuilder();
            foreach ((string name, (PropertyBase, ConfigAttribute) configEntry) in config.m_NameToConfig.OrderBy(key => key.Key))
                builder.Append(name).Append(Separator).Append(" ").AppendProperty(configEntry.Item1).Append("\n");
            string configPath = GetConfigFile();
            File.WriteAllText(configPath, builder.ToString());
#if UNITY_EDITOR
            Debug.Log($"[{_logTag}] Wrote config to {configPath}");
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
                        if (cells.Length == 0) continue;

                        string configName = cells[0], stringValue = cells.Length == 1 ? string.Empty : cells[1].Trim();
                        if (Active.m_NameToConfig.TryGetValue(configName, out (PropertyBase, ConfigAttribute) tuple))
                        {
                            (PropertyBase property, ConfigAttribute attribute) = tuple;
                            if (stringValue == "none") property.Clear();
                            else if (property.TryParseValue(stringValue)) Active.OnConfigUpdated(property, attribute, false);
                            else Debug.LogWarning($"[{_logTag}] Failed to parse config {configName} with value: {stringValue}");
                        }
                        else Debug.LogWarning($"[{_logTag}] Unrecognized config with name: {configName}");
                    }
                    OnActiveLoaded();
                }
                else
                {
                    WriteDefaults();
                    Debug.LogWarning($"[{_logTag}] Config file was not found so a default one was written");
                    SetActiveToDefault();
                }
#if UNITY_EDITOR
                Debug.Log($"[{_logTag}] Read config from {configPath}");
#endif
            }
            catch (Exception exception)
            {
                SetActiveToDefault();
                L.Exception(exception, $"[{_logTag}] Failed to parse config. Using defaults. Check permissions?");
            }
        }

        private static void OnActiveLoaded()
        {
            foreach (StringProperty command in Active.consoleHistory.List)
                ConsoleCommandExecutor.InsertPreviousCommand(command.AsNewString());
        }

        private static void SetActiveToDefault()
        {
            foreach ((string name, (PropertyBase, ConfigAttribute) config) in Default.Value.m_NameToConfig)
                Active.m_NameToConfig[name].Item1.SetTo(config.Item1);
        }

        public static void OnCommand(string command)
        {
            Active.consoleHistory.Append(new StringProperty(command));
            WriteActive();
        }
    }
}