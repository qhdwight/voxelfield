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
    [CreateAssetMenu(fileName = "Config", menuName = "Session/Config", order = 0)]
    public class ConfigManager : ScriptableObject
    {
        public static Dictionary<Type, PropertyBase> Configs { get; private set; }

        public static ConfigManager Singleton { get; private set; }

        public static Dictionary<string, PropertyBase> Variables { get; private set; }

        public static void Initialize()
        {
            Singleton = Resources.Load<ConfigManager>("Config");
            if (!Singleton) throw new Exception("No config asset was found in resources");
            IReadOnlyList<FieldInfo> fields = Singleton.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                                                       .Where(field => field.IsDefined(typeof(ConfigAttribute))).ToArray();
            Variables = fields.ToDictionary(field => field.GetCustomAttribute<ConfigAttribute>().Name, field => (PropertyBase) field.GetValue(Singleton));
            Configs = fields.ToDictionary(field => field.FieldType, field => (PropertyBase) field.GetValue(Singleton));
            foreach (KeyValuePair<string, PropertyBase> pair in Variables)
            {
                ConsoleCommandExecutor.SetCommand(pair.Key, args =>
                {
                    if (args.Length <= 1) return;
                    foreach (SessionBase session in SessionBase.Sessions)
                    {
                        session.StringCommand(session.GetLatestSession().Require<LocalPlayerId>(), string.Join(" ", args));
                    }
                    switch (pair.Value)
                    {
                        case ByteProperty byteProperty when byte.TryParse(args[1], out byte @byte):
                        {
                            byteProperty.Value = @byte;
                            break;
                        }
                        case BoolProperty boolProperty when bool.TryParse(args[1], out bool @bool):
                        {
                            boolProperty.Value = @bool;
                            break;
                        }
                    }
                });
            }
        }

        [Config("tick_rate", true)] public TickRateProperty tickRate;
        [Config("allow_cheats", true)] public AllowCheatsProperty allowCheats;
    }
}