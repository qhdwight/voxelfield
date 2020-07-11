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
        public static ConfigManager Singleton { get; private set; }
        private static Dictionary<string, FieldInfo> _variables;

        public static void Initialize()
        {
            Singleton = Resources.Load<ConfigManager>("Config");
            if (!Singleton) throw new Exception("No config asset was found in resources");
            _variables = Singleton.GetType().GetFields(BindingFlags.Instance | BindingFlags.Public)
                                  .Where(field => field.IsDefined(typeof(ConfigAttribute)))
                                  .ToDictionary(field => field.GetCustomAttribute<ConfigAttribute>().Name, field => field);
            foreach (KeyValuePair<string, FieldInfo> pair in _variables)
            {
                ConsoleCommandExecutor.SetCommand(pair.Key, args =>
                {
                    object property = pair.Value.GetValue(Singleton);
                    if (property is ByteProperty byteProperty && byte.TryParse(args[1], out byte @byte))
                        byteProperty.Value = @byte;
                });
            }
        }

        [Config("tick_rate", true)] public TickRateProperty tickRate;
    }
}