using System;
using System.Collections.Generic;
using System.Linq;
using Console.Interface;
using Input;
using UnityEngine;

namespace Console
{
    public static class ConsoleCommandExecutor
    {
        private static readonly string[] CommandSeparator = {"&&"};

        private static Dictionary<string, Action<string[]>> _commands;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            // Conform with https://docs.unity3d.com/Manual/DomainReloading.html
            _commands = new Dictionary<string, Action<string[]>>
            {
                ["clear"] = args => ConsoleInterface.Singleton.ClearConsole(),
                ["sensitivity"] = args =>
                {
                    if (float.TryParse(args[1], out float sensitivity))
                        InputProvider.Singleton.Sensitivity = sensitivity;
                    else
                        Debug.LogWarning($"Could not parse {args[1]} as float sensitivity");
                },
                ["volume"] = args =>
                {
                    if (float.TryParse(args[1], out float volume) && volume >= 0.0f && volume <= 1.0f)
                        AudioListener.volume = volume;
                    else
                        Debug.LogWarning($"Could not parse {args[1]} as volume");
                }
            };
        }

        public static void RegisterCommand(string commandName, Action<string[]> command) { _commands.Add(commandName, command); }

        public static string GetAutocomplete(string stub) { return _commands.Keys.FirstOrDefault(command => command.StartsWith(stub)); }

        public static void ExecuteCommand(string fullCommand)
        {
            string[] commands = fullCommand.Split(CommandSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string command in commands)
            {
                string[] args = command.Trim().Split();
                string commandName = args.First();
                if (_commands.ContainsKey(commandName))
                {
                    _commands[commandName](args);
                }
                else
                    Debug.LogWarning($"Command \"{commandName}\" not found!");
            }
        }
    }
}