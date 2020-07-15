using TMPro;
using UnityEngine;

namespace Voxelfield.Interface
{
    public class VersionInterface : MonoBehaviour
    {
        private TextMeshProUGUI m_Text;

        private void Awake()
        {
            m_Text = GetComponent<TextMeshProUGUI>();
            m_Text.SetText($"Version {Application.version} on Unity {Application.unityVersion}");
        }
    }
}