using Input;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using Voxelfield.Session;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface
{
    public class BuyMenuInterface : SessionInterfaceBehavior
    {
        private bool m_PlayerWantsVisible;
        private BuyMenuButton[] m_BuyButtons;
        private int? m_WantedBuyItemId;
        private AudioSource m_AudioSource;

        [SerializeField] private BufferedTextGui m_MoneyText = default;
        [SerializeField] private AudioClip m_HoverClip = default, m_PressClip = default;

        protected override void Awake()
        {
            base.Awake();
            m_BuyButtons = GetComponentsInChildren<BuyMenuButton>();
            m_AudioSource = GetComponent<AudioSource>();
        }

        private void Start()
        {
            foreach (BuyMenuButton button in m_BuyButtons)
            {
                button.OnClick.AddListener(() =>
                {
                    m_WantedBuyItemId = button.ItemId;
                    Play(m_PressClip);
                });
                button.OnEnter.AddListener(() => Play(m_HoverClip));
            }
        }

        private void Play(AudioClip clip)
        {
            m_AudioSource.pitch = Random.Range(0.95f, 1.05f);
            m_AudioSource.PlayOneShot(clip);
        }

        public override void SessionStateChange(bool isActive) => m_WantedBuyItemId = null;

        public override void ModifyLocalTrusted(int localPlayerId, SessionBase session, Container commands)
        {
            if (!m_WantedBuyItemId.HasValue) return;

            var itemId = checked((byte) m_WantedBuyItemId.Value);
            commands.Require<MoneyComponent>().wantedBuyItemId.Value = itemId;
            m_WantedBuyItemId = null;
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isActive = ShouldBeActive(session, sessionContainer, out Container localPlayer);

            if (isActive && InputProvider.Singleton.GetInputDown(InputType.Buy)) m_PlayerWantsVisible = !m_PlayerWantsVisible;
            isActive = isActive && m_PlayerWantsVisible;

            if (isActive && localPlayer.With(out MoneyComponent money) && money.count.WithValue)
            {
                m_MoneyText.StartBuild().Append("$").Append(money.count).Commit(m_MoneyText);
            }
            SetInterfaceActive(isActive);
        }

        private static bool ShouldBeActive(SessionBase session, Container sessionContainer, out Container sessionLocalPlayer)
        {
            sessionLocalPlayer = default;
            var localPlayerId = sessionContainer.Require<LocalPlayerId>();
            if (localPlayerId.WithoutValue) return false;

            if (ModeManager.GetMode(sessionContainer) is IModeWithBuying buyingMode)
            {
                sessionLocalPlayer = session.GetModifyingPayerFromId(localPlayerId);
                return buyingMode.CanBuy(session, sessionContainer, sessionLocalPlayer);
            }
            return false;
        }
    }
}