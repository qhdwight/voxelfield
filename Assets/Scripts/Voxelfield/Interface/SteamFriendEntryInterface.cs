using Swihoni.Util.Interface;
using UnityEngine;
using UnityEngine.UI;

namespace Voxelfield.Interface
{
    public class SteamFriendEntryInterface : MonoBehaviour
    {
        private BufferedTextGui m_Text;
        private RawImage m_Image;

        private void Awake()
        {
            m_Text = GetComponentInChildren<BufferedTextGui>();
            m_Image = GetComponentInChildren<RawImage>();
        }

        public void Render(Entry entry)
        {
            m_Text.SetText(entry.text);
            m_Text.ForceMeshUpdate();
            if (entry.texture) m_Image.texture = entry.texture;
        }
    }
}