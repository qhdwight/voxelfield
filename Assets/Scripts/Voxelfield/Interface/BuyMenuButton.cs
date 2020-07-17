using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items;
using Swihoni.Sessions.Modes;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Voxelfield.Session.Mode;

namespace Voxelfield.Interface
{
    public class BuyMenuButton : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private byte m_ItemId = default;
        [SerializeField] private TextMeshProUGUI m_NameText = default, m_CostText = default;
        private Button m_Button;

        public byte ItemId => m_ItemId;
        public UnityEvent OnEnter { get; } = new UnityEvent();
        public Button.ButtonClickedEvent OnClick => m_Button.onClick;

        private void Awake() => m_Button = GetComponent<Button>();

        private void Start()
        {
            var secureArea = (SecureAreaMode) ModeManager.GetMode(ModeIdProperty.SecureArea);
            ushort cost = secureArea.GetCost(m_ItemId);
            m_CostText.SetText(cost > 0 ? $"{cost:C0}" : string.Empty);
            m_NameText.SetText(ItemAssetLink.GetModifier(m_ItemId).itemName);
        }

        public void OnPointerEnter(PointerEventData eventData) => OnEnter.Invoke();
    }
}