using System.Collections.Generic;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Util.Interface;
using TMPro;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class ChatInterface : SessionInterfaceBehavior
    {
        [SerializeField] private BufferedTextGui m_Text = default;
        private TMP_InputField m_Input;
        private string m_WantedInput;

        private readonly Queue<string> m_Chats = new Queue<string>();

        protected override void Awake()
        {
            m_Input = GetComponentInChildren<TMP_InputField>();
            base.Awake();
            m_Input.onEndEdit.AddListener(chatString =>
            {
                m_WantedInput = chatString;
                m_Input.text = string.Empty;
            });
        }

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (string.IsNullOrEmpty(m_WantedInput)) return;

            commands.Require<ChatEntryProperty>().SetTo(m_WantedInput);
            m_WantedInput = null;
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            if (NoInterrupting && InputProvider.GetInputDown(InputType.ToggleChat) && !m_Input.isFocused)
                ToggleInterfaceActive();
        }

        public override void SetInterfaceActive(bool isActive)
        {
            base.SetInterfaceActive(isActive);
            if (isActive)
            {
                m_Input.enabled = true;
                m_Input.ActivateInputField();
                m_Input.Select();
            }
            else
            {
                m_Input.DeactivateInputField();
                m_Input.enabled = false;
            }
        }

        public override void SessionStateChange(bool isActive)
        {
            base.SessionStateChange(isActive);
            if (!isActive)
            {
                m_Chats.Clear();
                m_Text.Clear();
            }
        }

        public override void OnMostRecent(SessionBase session, Container sessionContainer)
        {
            if (sessionContainer.WithPropertyWithValue(out ChatListElement chats))
                foreach (ChatEntryProperty chat in chats.List)
                {
                    m_Chats.Enqueue(chat.AsNewString());
                    RenderChats();
                }
        }

        private void RenderChats()
        {
            StringBuilder builder = m_Text.StartBuild();
            foreach (string chat in m_Chats) builder.AppendLine(chat);
            builder.Commit(m_Text);
        }
    }
}