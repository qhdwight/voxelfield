using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Console;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using UnityEngine;

namespace Swihoni.Sessions
{
    public class ConfigAttribute : Attribute
    {
        public string Name { get; }
        public bool IsSession { get; }

        public ConfigAttribute(string name, bool isSession = false)
        {
            Name = name;
            IsSession = isSession;
        }
    }

    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class ConfigManager : ScriptableObject
    {
        public static Dictionary<Type, PropertyBase> TypeToConfig { get; private set; }

        public static ConfigManager Singleton { get; private set; }

        public static Dictionary<string, PropertyBase> NameToConfig { get; private set; }

        public static void Initialize()
        {
            Singleton = Resources.Load<ConfigManager>("Config");
            if (!Singleton) throw new Exception("No config asset was found in resources");
            IReadOnlyList<FieldInfo> fields = Singleton.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                                                       .Where(field => field.IsDefined(typeof(ConfigAttribute))).ToArray();
            NameToConfig = fields.ToDictionary(field => field.GetCustomAttribute<ConfigAttribute>().Name, field => (PropertyBase) field.GetValue(Singleton));
            TypeToConfig = fields.Where(field => field.GetCustomAttribute<ConfigAttribute>().IsSession)
                                 .ToDictionary(field => field.FieldType, field => (PropertyBase) field.GetValue(Singleton));
            foreach (ConfigAttribute attribute in fields.Select(field => field.GetCustomAttribute<ConfigAttribute>()))
            {
                ConsoleCommandExecutor.SetCommand(attribute.Name, args =>
                {
                    if (args.Length == 2)
                    {
                        if (attribute.IsSession)
                            foreach (Client session in SessionBase.Sessions.OfType<Client>())
                                session.StringCommand(session.GetLatestSession().Require<LocalPlayerId>(), string.Join(" ", args));
                        HandleArgs(args);
                    }
                });
            }
        }

        public static void TryCommand(StringCommandProperty command)
        {
            if (command.Builder.Length > 0)
            {
                string[] split = command.Builder.ToString().Trim().Split();
                HandleArgs(split);
            }
        }

        private static void HandleArgs(IReadOnlyList<string> split)
        {
            if (split.Count == 2 && NameToConfig.TryGetValue(split[0], out PropertyBase property) && property.TryParseValue(split[1]))
                Debug.Log($"Set {split[0]} to {split[1]}");
        }

        [Config("tick_rate", true)] public TickRateProperty tickRate = new TickRateProperty(60);
        [Config("allow_cheats", true)] public AllowCheatsProperty allowCheats;
        [Config("mode_id", true)] public ModeIdProperty modeId;
        
        [Config("fov")] public ByteProperty fov = new ByteProperty(60);
        [Config("target_fps")] public UShortProperty targetFps = new UShortProperty(200);
        [Config("volume")] public FloatProperty volume = new FloatProperty(0.5f);
        [Config("sensitivity")] public FloatProperty sensitivity = new FloatProperty(2.0f);
    }
}