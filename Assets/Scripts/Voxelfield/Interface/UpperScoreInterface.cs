using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityEngine.UI;
using Voxelfield.Session;

namespace Voxelfield.Interface
{
    public class UpperScoreInterface : SessionInterfaceBehavior
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
            m_LeftText.StartBuild().Append(leftScore).Commit(m_LeftText);
            m_RightText.StartBuild().Append(rightScore).Commit(m_RightText);
            leftColor.a = rightColor.a = 0.8f;
            m_LeftScore.color = leftColor;
            m_RightScore.color = rightColor;
        }

        public override void Render(SessionBase session, Container sessionContainer)
        {
            bool isVisible = sessionContainer.With(out DualScoresComponent dualScores) && dualScores[0].WithValue && dualScores[1].WithValue && !session.IsLoading;
            if (isVisible)
            {
                ModeBase mode = ModeManager.GetMode(sessionContainer);
                Render(dualScores[0], mode.GetTeamColor(0), dualScores[1], mode.GetTeamColor(1));
            }
            SetInterfaceActive(isVisible);
        }
    }
}