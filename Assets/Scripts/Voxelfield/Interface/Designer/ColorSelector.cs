using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Voxelfield.Interface.Designer
{
    public class ColorSelector : MonoBehaviour
    {
        private Slider m_Slider;
        [SerializeField] private TextMeshProUGUI m_ValueText = default;
        private Image m_FillImage = default, m_HandleImage = default;

        public Slider.SliderEvent OnValueChanged => m_Slider.onValueChanged;
        
        public int Index { get; set; }
        
        private void Awake()
        {
            m_Slider = GetComponentInChildren<Slider>();
            m_Slider.onValueChanged.AddListener(floatValue => m_ValueText.SetText(Mathf.RoundToInt(floatValue).ToString()));
            m_FillImage = m_Slider.fillRect.GetComponent<Image>();
            m_HandleImage = m_Slider.handleRect.GetComponent<Image>();
        }

        public void SetColor(in Color? _color)
        {
            Color color = _color.GetValueOrDefault(Color.black);
            
            m_HandleImage.color = color;
            m_FillImage.color = color;
            m_Slider.value = color[Index];
        }
    }
}