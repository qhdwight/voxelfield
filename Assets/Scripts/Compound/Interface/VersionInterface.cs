using TMPro;
using UnityEngine;

namespace Compound.Interface
{
    public class VersionInterface : MonoBehaviour
    {
        private TextMeshProUGUI m_Text;

        private void Awake()
        {
            m_Text = GetComponent<TextMeshProUGUI>();
            m_Text.SetText($"Version {Version.String} on Unity {Application.unityVersion}");
        }
    }
}