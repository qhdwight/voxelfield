using System;
using System.Collections.Generic;
using System.Text;
using Input;
using Swihoni.Util.Interface;
using TMPro;
using UnityEngine;

namespace Console.Interface
{
    public class ConsoleInterface : SingletonInterfaceBehavior<ConsoleInterface>
    {
        private const int MaxPreviousCommands = 20;

        private struct LogItem
        {
            public string logString;
            public LogType type;
        }

        private static readonly Dictionary<LogType, string> LogColors = new Dictionary<LogType, string>
        {
            [LogType.Error] = "red",
            [LogType.Assert] = "green",
            [LogType.Exception] = "red",
            [LogType.Warning] = "yellow",
            [LogType.Log] = "white"
        };

        [SerializeField] private int m_MaxMessages = 100;
        [SerializeField] private TextMeshProUGUI m_ConsoleText = default;
        [SerializeField] private TMP_InputField m_ConsoleInput = default;
        [SerializeField] private Color m_AutocompleteColor = default;

        private string m_AutocompleteColorHex;
        private readonly Queue<LogItem> m_LogItems = new Queue<LogItem>();
        private readonly StringBuilder m_LogBuilder = new StringBuilder();
        private readonly List<string> m_PreviousCommands = new List<string>(MaxPreviousCommands + 1);
        private int m_CommandHistoryIndex;
        private string m_CurrentAutocomplete;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            // Conform with https://docs.unity3d.com/Manual/DomainReloading.html
            Application.logMessageReceived -= Singleton.Log;
        }

        protected override void Awake()
        {
            base.Awake();
            m_AutocompleteColorHex = $"#{ColorUtility.ToHtmlStringRGBA(m_AutocompleteColor)}";
            m_ConsoleInput.onSubmit.AddListener(ConsoleInput);
            m_ConsoleInput.onValueChanged.AddListener(ConsoleInputChange);
//            m_ConsoleInput.onValidateInput += OnValidateInput;
        }

        private void Start() => Application.logMessageReceived += Log;

        //        private static char OnValidateInput(string text, int charIndex, char addedChar)
//        {
//            return Regex.IsMatch(addedChar.ToString(), @"^\w+$") ? addedChar : '\0';
////            return addedChar == '`' ? '\0' : addedChar;
//        }

        public override void SetInterfaceActive(bool isActive)
        {
            base.SetInterfaceActive(isActive);
            if (isActive)
            {
                m_ConsoleInput.ActivateInputField();
                m_ConsoleInput.Select();
            }
            else
                m_ConsoleInput.DeactivateInputField();
        }

        public void ClearConsole()
        {
            m_LogItems.Clear();
            SetLogText();
        }

        private void Update()
        {
            InputProvider input = InputProvider.Singleton;
            if (input.GetInputDown(InputType.ToggleConsole))
                ToggleInterfaceActive();
            if (input.GetInputDown(InputType.AutocompleteConsole))
            {
                if (!string.IsNullOrEmpty(m_CurrentAutocomplete))
                {
                    m_ConsoleInput.SetTextWithoutNotify(m_CurrentAutocomplete);
                    m_ConsoleInput.MoveToEndOfLine(false, false);
                }
            }
            bool focused = m_ConsoleInput.isFocused;
            if (focused)
            {
                bool wantsNextCommand =
                         input.GetInputDown(InputType.PreviousConsoleCommand) &&
                         m_CommandHistoryIndex + 1 < m_PreviousCommands.Count,
                     wantsPreviousCommand =
                         input.GetInputDown(InputType.NextConsoleCommand) &&
                         m_CommandHistoryIndex - 1 >= 0;
                if (!wantsNextCommand && !wantsPreviousCommand) return;
                m_ConsoleInput.text = wantsNextCommand
                    ? m_PreviousCommands[++m_CommandHistoryIndex]
                    : m_PreviousCommands[--m_CommandHistoryIndex];
                m_ConsoleInput.MoveTextEnd(false);
            }
            else
                m_CommandHistoryIndex = -1;
        }

        private void Log(string logString, string stackTrace, LogType type)
        {
            while (m_LogItems.Count > m_MaxMessages)
                m_LogItems.Dequeue();
            m_LogItems.Enqueue(new LogItem {logString = logString, type = type});
            SetLogText();
        }

        private void SetLogText()
        {
            m_LogBuilder.Clear();
            foreach (LogItem logItem in m_LogItems)
            {
                m_LogBuilder.AppendFormat("<color={0}>{1}</color>", LogColors[logItem.type], logItem.logString);
                m_LogBuilder.AppendLine();
            }
            m_ConsoleText.SetText(m_LogBuilder);
        }

        private void ConsoleInputChange(string consoleInput)
        {
            if (string.IsNullOrEmpty(consoleInput)) return;
            consoleInput = StripAutocomplete(consoleInput, out int indexOfAutocomplete);
            string autoComplete = ConsoleCommandExecutor.GetAutocomplete(consoleInput);
            m_CurrentAutocomplete = autoComplete;
            if (indexOfAutocomplete == 0)
            {
                m_ConsoleInput.SetTextWithoutNotify(string.Empty);
                return;
            }
            if (autoComplete == null)
            {
                if (indexOfAutocomplete > 0)
                    m_ConsoleInput.SetTextWithoutNotify(consoleInput);
            }
            else
                m_ConsoleInput.SetTextWithoutNotify($"{consoleInput}<color={m_AutocompleteColorHex}>{autoComplete.Substring(consoleInput.Length)}</color>");
        }

        private string StripAutocomplete(string input, out int indexOfAutocomplete)
        {
            indexOfAutocomplete = input.IndexOf($"<color={m_AutocompleteColorHex}>", StringComparison.Ordinal);
            if (indexOfAutocomplete > 0) input = input.Substring(0, indexOfAutocomplete);
            return input;
        }

        private void ConsoleInput(string consoleInput)
        {
            consoleInput = StripAutocomplete(consoleInput, out _);
            if (string.IsNullOrWhiteSpace(consoleInput)) return;

            ConsoleCommandExecutor.ExecuteCommand(consoleInput);
            m_PreviousCommands.Insert(0, consoleInput);
            if (m_PreviousCommands.Count > MaxPreviousCommands)
                m_PreviousCommands.RemoveAt(m_PreviousCommands.Count - 1);
            m_ConsoleInput.text = string.Empty;
            m_ConsoleInput.ActivateInputField();
            m_ConsoleInput.Select();
        }
    }
}