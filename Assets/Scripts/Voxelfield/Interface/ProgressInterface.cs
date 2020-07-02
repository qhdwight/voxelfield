using Swihoni.Util;
using Swihoni.Util.Interface;
using UnityEngine.UI;

namespace Voxelfield.Interface
{
    public class ProgressInterface : InterfaceBehaviorBase
    {
        private Slider m_Slider;
        private BufferedTextGui m_Text;

        protected override void Awake()
        {
            base.Awake();
            m_Slider = GetComponentInChildren<Slider>();
            m_Text = GetComponentInChildren<BufferedTextGui>();
        }

        public void Set(uint timeUs, uint maxTimeUs)
        {
            m_Slider.value = (float) (timeUs / (decimal) maxTimeUs);
            m_Text.BuildText(builder => builder.AppendFormat("{0:F1} Seconds", timeUs * TimeConversions.MicrosecondToSecond));
        }
    }
}