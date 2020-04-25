using System;
using System.Collections.Generic;
using System.Linq;
using Console.Interface;
using UnityEngine;

namespace Console
{
    public delegate string Command(string[] args);

    public static class ConsoleCommandExecutor
    {
        private static readonly string[] CommandSeparator = {"&&"};

        private static Dictionary<string, Command> _commands;

        [RuntimeInitializeOnLoadMethod]
        private static void RunOnStart()
        {
            // Conform with https://docs.unity3d.com/Manual/DomainReloading.html
            _commands = new Dictionary<string, Command>
            {
                ["clear"] = args =>
                {
                    ConsoleInterface.Singleton.ClearConsole();
                    return null;
                }
            };
        }

        public static void RegisterCommand(string commandName, Command command) { _commands.Add(commandName, command); }

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
                    string result = _commands[commandName](args);
                    if (!string.IsNullOrEmpty(result))
                        Debug.Log(result);
                }
                else
                    Debug.LogWarning($"Command \"{commandName}\" not found!");
            }
        }
    }
}