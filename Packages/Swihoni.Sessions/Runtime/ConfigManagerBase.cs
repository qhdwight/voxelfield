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
    public class ConfigManagerBase : ScriptableObject
    {
        public static Dictionary<Type, (PropertyBase, ConfigAttribute)> TypeToConfig { get; private set; }

        public static ConfigManagerBase Singleton { get; private set; }

        public static Dictionary<string, (PropertyBase, ConfigAttribute)> NameToConfig { get; private set; }

        public static void Initialize()
        {
            Singleton = Resources.Load<ConfigManagerBase>("Config");
            if (!Singleton) throw new Exception("No config asset was found in resources");
            IReadOnlyList<FieldInfo> fields = Singleton.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                                                       .Where(field => field.IsDefined(typeof(ConfigAttribute))).ToArray();

            (PropertyBase, ConfigAttribute) ValueSelector(FieldInfo field) => ((PropertyBase) field.GetValue(Singleton), field.GetCustomAttribute<ConfigAttribute>());
            NameToConfig = fields.ToDictionary(field => field.GetCustomAttribute<ConfigAttribute>().Name, ValueSelector);
            TypeToConfig = NameToConfig.Values.Where(tuple => tuple.Item2.IsSession)
                                       .ToDictionary(tuple => tuple.Item1.GetType(), tuple => tuple);
            foreach ((PropertyBase property, ConfigAttribute attribute) in NameToConfig.Values)
            {
                if (property.WithoutValue) property.Zero();
                ConsoleCommandExecutor.SetCommand(attribute.Name, args =>
                {
                    if (attribute.IsSession)
                        foreach (Client session in SessionBase.Sessions.OfType<Client>())
                            session.StringCommand(session.GetLatestSession().Require<LocalPlayerId>(), string.Join(" ", args));
                    HandleArgs(args);
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
            if (NameToConfig.TryGetValue(split[0], out (PropertyBase, ConfigAttribute) tuple))
            {
                switch (split.Count)
                {
                    case 2:
                        tuple.Item1.TryParseValue(split[1]);
                        Debug.Log($"Set {split[0]} to {split[1]}");
                        break;
                    case 1 when tuple.Item1 is BoolProperty boolProperty:
                        boolProperty.Value = true;
                        Debug.Log($"Set {split[0]}");
                        break;
                }
            }
        }

        [Config("tick_rate", true)] public TickRateProperty tickRate = new TickRateProperty(60);
        [Config("allow_cheats", true)] public AllowCheatsProperty allowCheats = new AllowCheatsProperty();
        [Config("mode_id", true)] public ModeIdProperty modeId = new ModeIdProperty();

        [Config("fov")] public ByteProperty fov = new ByteProperty(60);
        [Config("target_fps")] public UShortProperty targetFps = new UShortProperty(200);
        [Config("volume")] public FloatProperty volume = new FloatProperty(0.5f);
        [Config("sensitivity")] public FloatProperty sensitivity = new FloatProperty(2.0f);

        public static void UpdateConfig(ComponentBase session)
        {
            foreach (ElementBase element in session.Elements)
                if (element is PropertyBase property && TypeToConfig.TryGetValue(property.GetType(), out (PropertyBase, ConfigAttribute) tuple))
                    property.SetFromIfWith(tuple.Item1);
        }
    }
}