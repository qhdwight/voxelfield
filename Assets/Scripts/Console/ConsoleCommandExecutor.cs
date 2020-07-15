using System;
using System.Collections.Generic;
using System.Linq;
using Console.Interface;
using UnityEngine;

namespace Console
{
    public static class ConsoleCommandExecutor
    {
        private static readonly string[] CommandSeparator = {"&&"};

        private static Dictionary<string, Action<string[]>> _commands;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        private static void Initialize()
        {
            // Conform with https://docs.unity3d.com/Manual/DomainReloading.html
            _commands = new Dictionary<string, Action<string[]>>
            {
                ["clear"] = args => ConsoleInterface.Singleton.ClearConsole(),
                ["quit"] = _ => Application.Quit()
            };
        }

        public static void SetCommand(string commandName, Action<string[]> command) => _commands[commandName] = command;

        public static void SetAlias(string alias, string realCommand) => _commands[alias] = args => ExecuteCommand(realCommand);

        public static string GetAutocomplete(string stub) => _commands.Keys.FirstOrDefault(command => command.StartsWith(stub));

        public static void ExecuteCommand(string fullCommand)
        {
            string[] commands = fullCommand.Split(CommandSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string command in commands)
            {
                string[] args = command.Trim().Split();
                string commandName = args.First();
                if (_commands.ContainsKey(commandName)) _commands[commandName](args);
                else Debug.LogWarning($"Command \"{commandName}\" not found!");
            }
        }

        public static void RemoveCommand(string command) => _commands.Remove(command);

        public static void RemoveCommands(params string[] commands)
        {
            foreach (string command in commands) RemoveCommand(command);
        }
    }
}