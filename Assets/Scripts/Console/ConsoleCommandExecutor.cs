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

        private static readonly Dictionary<string, Command> Commands = new Dictionary<string, Command>
        {
            ["clear"] = args =>
            {
                ConsoleInterface.Singleton.ClearConsole();
                return null;
            }
        };

        public static string GetAutocomplete(string stub)
        {
            return Commands.Keys.FirstOrDefault(command => command.StartsWith(stub));
        }

        public static void ExecuteCommand(string fullCommand)
        {
            string[] commands = fullCommand.Split(CommandSeparator, StringSplitOptions.RemoveEmptyEntries);
            foreach (string command in commands)
            {
                string[] args = command.Trim().Split();
                string commandName = args.First();
                if (Commands.ContainsKey(commandName))
                {
                    string result = Commands[commandName](args);
                    if (!string.IsNullOrEmpty(result))
                        Debug.Log(result);
                }
                else
                    Debug.LogWarning($"Command \"{commandName}\" not found!");
            }
        }
    }
}