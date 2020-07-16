using Swihoni.Sessions.Items;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace Voxelfield.Interface
{
    [RequireComponent(typeof(Button))]
    public class LoadOutButton : MonoBehaviour
    {
        [SerializeField] private Image m_CheckMarkImage = default;
        [SerializeField] private byte m_ItemId = default;
        private TextMeshProUGUI m_NameText;
        private Button m_Button;

        public byte ItemId => m_ItemId;
        public UnityEvent OnClick => m_Button.onClick;
        
        private void Awake()
        {
            m_NameText = GetComponentInChildren<TextMeshProUGUI>();
            m_Button = GetComponentInChildren<Button>();
        }

        private void Start() => m_NameText.SetText(ItemAssetLink.GetModifier(m_ItemId).itemName);

        public void SetChecked(bool isChecked) => m_CheckMarkImage.gameObject.SetActive(isChecked);
    }
}