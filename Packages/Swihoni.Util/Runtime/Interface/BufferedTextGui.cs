using System.Text;
using TMPro;

namespace Swihoni.Util.Interface
{
    public class BufferedTextGui : TextMeshProUGUI
    {
        public StringBuilder Builder { get; } = new StringBuilder(1 << 5);

        public StringBuilder StartBuild()
        {
            Builder.Clear();
            return Builder;
        }

        public void Clear()
        {
            Builder.Clear();
            Commit();
        }

        public void Commit() => SetText(Builder);
    }

    public static class BuilderExtension
    {
        public static void Commit(this StringBuilder builder, TMP_Text text) => text.SetText(builder);
    }
}