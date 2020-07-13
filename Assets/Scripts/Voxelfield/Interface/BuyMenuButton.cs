using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Voxelfield.Interface
{
    public class BuyMenuButton : MonoBehaviour, IPointerEnterHandler
    {
        [SerializeField] private byte m_ItemId = default;
        private Button m_Button;

        public byte ItemId => m_ItemId;
        public UnityEvent OnEnter { get; } = new UnityEvent();
        public Button.ButtonClickedEvent OnClick => m_Button.onClick;

        private void Awake() => m_Button = GetComponent<Button>();

        public void OnPointerEnter(PointerEventData eventData) => OnEnter.Invoke();
    }
}