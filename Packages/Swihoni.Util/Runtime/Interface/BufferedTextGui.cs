using System.Text;
using TMPro;

namespace Swihoni.Util.Interface
{
    public class BufferedTextGui : TextMeshProUGUI
    {
        private readonly StringBuilder m_Builder = new StringBuilder(1 << 5);

        public StringBuilder StartBuild()
        {
            m_Builder.Clear();
            return m_Builder;
        }

        // public void BuildText(Action<StringBuilder> build)
        // {
        //     m_Builder.Clear();
        //     build(m_Builder);
        //     SetText(m_Builder);
        // }
    }

    public static class BuilderExtension
    {
        public static void Commit(this StringBuilder builder, BufferedTextGui text) => text.SetText(builder);
    }
}