using System;
using System.Collections.Generic;
using System.Linq;
using Swihoni.Sessions.Interfaces;
using UnityEngine;

namespace Swihoni.Sessions.Config
{
    public static class ConsoleCommandExecutor
    {
        private static readonly string[] CommandSeparator = {"&&"};

        private const int MaxPreviousCommands = 48;

        public static List<string> PreviousCommands { get; } = new List<string>(MaxPreviousCommands);
        
        private static readonly Dictionary<string, Action<string[]>> Commands = new Dictionary<string, Action<string[]>>
        {
            ["clear"] = args => ConsoleInterface.Singleton.ClearConsole(),
            ["quit"] = _ => Application.Quit()
        };

        static ConsoleCommandExecutor() => Commands["help"] = _ =>
        {
            string allCommands = string.Join(", ", Commands.Keys.OrderBy(name => name));
            Debug.Log($"Available commands:\n{allCommands}");
        };

        public static void BackLoadPreviousCommand(string command)
        {
            while (PreviousCommands.Count > MaxPreviousCommands - 1) PreviousCommands.RemoveAt(PreviousCommands.Count - 1);
            PreviousCommands.Add(command);
        }
        
        public static void InsertPreviousCommand(string command)
        {
            PreviousCommands.Insert(0, command);
            while (PreviousCommands.Count > MaxPreviousCommands) PreviousCommands.RemoveAt(PreviousCommands.Count - 1);
        }

        public static void SetCommand(string commandName, Action<string[]> command) => Commands[commandName] = command;

        public static void SetAlias(string alias, string realCommand) => Commands[alias] = args => ExecuteCommand(realCommand);

        public static string GetAutocomplete(string stub) => Commands.Keys.FirstOrDefault(command => command.StartsWith(stub));

        public static void ExecuteCommand(string fullCommand)
        {
            ConfigManagerBase.OnCommand(fullCommand);
            foreach (string[] args in GetArgs(fullCommand))
            {
                string commandName = args.First().Replace("?", string.Empty);
                if (Commands.ContainsKey(commandName))
                {
                    try
                    {
                        Commands[commandName](args);
                    }
                    catch (Exception)
                    {
                        Debug.Log($"Exception running command: {args[0]}");
                        throw;
                    }
                }
                else Debug.LogWarning($"Command \"{args[0]}\" not found!");
            }
        }

        public static IEnumerable<string[]> GetArgs(string fullCommand)
        {
            string[] commands = fullCommand.Split(CommandSeparator, StringSplitOptions.RemoveEmptyEntries);
            return commands.Select(command => command.Trim().Split()).ToList();
        }

        public static void RemoveCommand(string command) => Commands.Remove(command);

        public static void RemoveCommands(params string[] commands)
        {
            foreach (string command in commands) RemoveCommand(command);
        }
    }
}