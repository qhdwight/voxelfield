using Input;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface.Showdown
{
    public class BuyMenuInterface : SessionInterfaceBehavior
    {
        private bool m_PlayerWantsVisible;
        private BuyMenuButton[] m_BuyButtons;
        private int? m_WantedBuyItemId;

        [SerializeField] private BufferedTextGui m_MoneyText;

        protected override void Awake()
        {
            base.Awake();
            m_BuyButtons = GetComponentsInChildren<BuyMenuButton>();
            foreach (BuyMenuButton button in m_BuyButtons)
                button.Button.onClick.AddListener(() => OnBuyButtonClicked(button));
        }

        public override void SessionStateChange(bool isActive) => m_WantedBuyItemId = null;

        private void OnBuyButtonClicked(BuyMenuButton button) => m_WantedBuyItemId = button.ItemId;

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (!m_WantedBuyItemId.HasValue) return;
            
            var itemId = checked((byte) m_WantedBuyItemId.Value);
            commands.Require<MoneyComponent>().wantedBuyItemId.Value = itemId;
            m_WantedBuyItemId = null;
            Debug.Log($"Requesting item ID: {itemId}");
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isActive = IsActive(session, sessionContainer, out Container localPlayer, out ShowdownSessionComponent showdown);

            if (isActive && InputProvider.Singleton.GetInputDown(InputType.Buy)) m_PlayerWantsVisible = !m_PlayerWantsVisible;
            isActive = isActive && m_PlayerWantsVisible;

            if (isActive)
            {
                m_MoneyText.BuildText(builder => builder.Append("$").Append(localPlayer.Require<MoneyComponent>().count));
            }
            SetInterfaceActive(isActive);
        }

        private static bool IsActive(SessionBase session, Container sessionContainer, out Container sessionLocalPlayer, out ShowdownSessionComponent sessionShowdown)
        {
            sessionLocalPlayer = default;
            sessionShowdown = default;
            
            var showdownMode = session.GetMode(sessionContainer) as ShowdownMode;
            if (showdownMode == null) return false;

            sessionShowdown = sessionContainer.Require<ShowdownSessionComponent>();
            if (sessionShowdown.number.WithValue && sessionShowdown.remainingUs <= ShowdownMode.FightTimeUs) return false;

            var localPlayerId = sessionContainer.Require<LocalPlayerId>();
            if (localPlayerId.WithoutValue) return false;

            sessionLocalPlayer = session.GetPlayerFromId(localPlayerId);
            return sessionLocalPlayer.Without(out HealthProperty localHealth) || localHealth.WithValue && localHealth.IsAlive;
        }
    }
}