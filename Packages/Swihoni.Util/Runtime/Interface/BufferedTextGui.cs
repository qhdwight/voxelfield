using System;
using System.Text;
using TMPro;

namespace Swihoni.Util.Interface
{
    public class BufferedTextGui : TextMeshProUGUI
    {
        private readonly StringBuilder m_Builder = new StringBuilder(1 << 5);

        public void SetText(Action<StringBuilder> build)
        {
            m_Builder.Clear();
            build(m_Builder);
            SetText(m_Builder);
        }
    }
}