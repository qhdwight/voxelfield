using System;
using System.Collections.Generic;
using System.Text;
using Swihoni.Sessions.Config;
using Swihoni.Util.Interface;
using TMPro;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class ConsoleInterface : SingletonInterfaceBehavior<ConsoleInterface>
    {
        private struct LogItem
        {
            public string logString;
            public LogType type;
        }

        private static readonly Dictionary<LogType, string> LogColors = new()
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
        private readonly Queue<LogItem> m_LogItems = new();
        private readonly StringBuilder m_LogBuilder = new();
        private int m_CommandHistoryIndex;
        private string m_CurrentAutocomplete;
        private bool m_OpenedForCommand, m_NeedsTextUpdate;

        [RuntimeInitializeOnLoadMethod]
        private static void Initialize()
        {
            // Conform with https://docs.unity3d.com/Manual/DomainReloading.html
            Application.logMessageReceived -= Singleton.Log;
            Application.logMessageReceived += Singleton.Log;
        }

        protected override void Awake()
        {
            base.Awake();
            m_AutocompleteColorHex = $"#{ColorUtility.ToHtmlStringRGBA(m_AutocompleteColor)}";
            m_ConsoleInput.onSubmit.AddListener(ConsoleInput);
            m_ConsoleInput.onValueChanged.AddListener(ConsoleInputChange);
//            m_ConsoleInput.onValidateInput += OnValidateInput;
        }

        //        private static char OnValidateInput(string text, int charIndex, char addedChar)
//        {
//            return Regex.IsMatch(addedChar.ToString(), @"^\w+$") ? addedChar : '\0';
////            return addedChar == '`' ? '\0' : addedChar;
//        }

        protected override void OnSetInterfaceActive(bool isActive)
        {
            if (isActive)
            {
                m_ConsoleInput.enabled = true;
                m_ConsoleInput.ActivateInputField();
                m_ConsoleInput.Select();
            }
            else
            {
                m_ConsoleInput.DeactivateInputField();
                m_OpenedForCommand = false;
                m_ConsoleInput.enabled = false;
            }
        }

        public void ClearConsole()
        {
            m_LogItems.Clear();
            RequestLogTextUpdate();
        }

        private void Update()
        {
            if (InputProvider.GetInputDown(InputType.ToggleConsole))
                ToggleInterfaceActive();
            else if (InputProvider.GetInputDown(InputType.ConsoleCommand))
            {
                SetInterfaceActive(!IsActive);
                if (IsActive) m_OpenedForCommand = true;
            }

            if (IsActive && m_NeedsTextUpdate)
            {
                m_ConsoleText.SetText(m_LogBuilder);
                m_NeedsTextUpdate = false;
            }

            if (InputProvider.GetInputDown(InputType.AutocompleteConsole))
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
                         InputProvider.GetInputDown(InputType.PreviousConsoleCommand) &&
                         m_CommandHistoryIndex + 1 < ConsoleCommandExecutor.PreviousCommands.Count,
                     wantsPreviousCommand =
                         InputProvider.GetInputDown(InputType.NextConsoleCommand) &&
                         m_CommandHistoryIndex - 1 >= 0;
                if (wantsNextCommand)
                    m_ConsoleInput.text = ConsoleCommandExecutor.PreviousCommands[++m_CommandHistoryIndex];
                else if (wantsPreviousCommand)
                    m_ConsoleInput.text = ConsoleCommandExecutor.PreviousCommands[--m_CommandHistoryIndex];
                if (wantsNextCommand || wantsPreviousCommand)
                    m_ConsoleInput.MoveTextEnd(false);
            }
            else m_CommandHistoryIndex = -1;
        }

        private void Log(string logString, string stackTrace, LogType type)
        {
            while (m_LogItems.Count > m_MaxMessages)
                m_LogItems.Dequeue();
            m_LogItems.Enqueue(new LogItem {logString = logString, type = type});
            RequestLogTextUpdate();
        }

        private void RequestLogTextUpdate()
        {
            m_LogBuilder.Clear();
            foreach (LogItem logItem in m_LogItems)
            {
                m_LogBuilder.AppendFormat("<color={0}>{1}</color>", LogColors[logItem.type], logItem.logString);
                m_LogBuilder.AppendLine();
            }
            m_NeedsTextUpdate = true;
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
            if (indexOfAutocomplete > 0) input = input[..indexOfAutocomplete];
            return input;
        }

        private void ConsoleInput(string consoleInput)
        {
            if (m_ConsoleInput.wasCanceled) return;

            consoleInput = StripAutocomplete(consoleInput, out _);
            if (string.IsNullOrWhiteSpace(consoleInput)) return;

            ConsoleCommandExecutor.ExecuteCommand(consoleInput);
            ConsoleCommandExecutor.InsertPreviousCommand(consoleInput);
            m_ConsoleInput.text = string.Empty;
            m_ConsoleInput.ActivateInputField();
            m_ConsoleInput.Select();

            if (m_OpenedForCommand) SetInterfaceActive(false);
        }
    }
}