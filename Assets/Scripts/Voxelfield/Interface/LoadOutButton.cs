using Swihoni.Sessions.Items;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Voxelfield.Interface
{
    public class LoadOutButton : MonoBehaviour
    {
        [SerializeField] private Image m_CheckMarkImage = default;
        [SerializeField] private byte m_ItemId = default;
        private TextMeshProUGUI m_NameText;

        private void Awake() => m_NameText = GetComponentInChildren<TextMeshProUGUI>();

        private void Start() => m_NameText.SetText(ItemAssetLink.GetModifier(m_ItemId).itemName);

        public void SetChecked(bool isChecked) => m_CheckMarkImage.gameObject.SetActive(isChecked);
    }
}