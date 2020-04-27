using System;
using System.Text;
using TMPro;

namespace Swihoni.Sessions.Interfaces
{
    public class BufferedTextGui : TextMeshProUGUI
    {
        private readonly StringBuilder m_Builder = new StringBuilder(1 << 5);

        public void Set(Action<StringBuilder> build)
        {
            m_Builder.Clear();
            build(m_Builder);
            SetText(m_Builder);
        }
    }
}