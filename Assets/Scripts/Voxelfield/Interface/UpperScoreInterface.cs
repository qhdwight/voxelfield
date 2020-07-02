using Swihoni.Util.Interface;
using UnityEngine;
using UnityEngine.UI;

namespace Voxelfield.Interface
{
    public class UpperScoreInterface : InterfaceBehaviorBase
    {
        [SerializeField] private Image m_LeftScore = default, m_RightScore = default;
        private BufferedTextGui m_LeftText, m_RightText;

        protected override void Awake()
        {
            base.Awake();
            m_LeftText = m_LeftScore.GetComponentInChildren<BufferedTextGui>();
            m_RightText = m_RightScore.GetComponentInChildren<BufferedTextGui>();
        }

        public void Render(int leftScore, Color leftColor, int rightScore, Color rightColor)
        {
            m_LeftText.BuildText(builder => builder.Append(leftScore));
            m_RightText.BuildText(builder => builder.Append(rightScore));
            leftColor.a = rightColor.a = 0.8f;
            m_LeftScore.color = leftColor;
            m_RightScore.color = rightColor;
        }
    }
}