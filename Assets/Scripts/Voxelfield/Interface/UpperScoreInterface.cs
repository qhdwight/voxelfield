using System.Linq;
using Swihoni.Components;
using Swihoni.Sessions;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Interfaces;
using Swihoni.Sessions.Modes;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;
using UnityEngine.UI;
using Voxelfield.Session;

namespace Voxelfield.Interface
{
    public class UpperScoreInterface : SessionInterfaceBehavior
    {
        private const int LeftTeam = 0, RightTeam = 1;

        [SerializeField] private Image m_LeftScore = default, m_RightScore = default;
        [SerializeField] private Image m_PlayerImagePrefab = default;
        private BufferedTextGui m_LeftText, m_RightText;
        private Image[][] m_PlayerImages;

        protected override void Awake()
        {
            base.Awake();
            m_LeftText = m_LeftScore.GetComponentInChildren<BufferedTextGui>();
            m_RightText = m_RightScore.GetComponentInChildren<BufferedTextGui>();
            m_PlayerImages = Enumerable.Range(0, 2).Select(team => Enumerable.Range(0, SessionBase.MaxPlayers / 2).Select(_ =>
            {
                Image image = Instantiate(m_PlayerImagePrefab, transform);
                if (team == LeftTeam) image.transform.SetAsFirstSibling();
                else image.transform.SetAsLastSibling();
                return image;
            }).ToArray()).ToArray();
        }

        public void Render(int leftScore, Color leftColor, int rightScore, Color rightColor)
        {
            m_LeftText.StartBuild().Append(leftScore).Commit(m_LeftText);
            m_RightText.StartBuild().Append(rightScore).Commit(m_RightText);
            leftColor.a = rightColor.a = 0.8f;
            m_LeftScore.color = leftColor;
            m_RightScore.color = rightColor;
        }

        public override void Render(in SessionContext context)
        {
            bool isVisible = context.sessionContainer.With(out DualScoresComponent dualScores) && dualScores[LeftTeam].WithValue && dualScores[RightTeam].WithValue && !context.session.IsLoading;
            if (isVisible)
            {
                ModeBase mode = context.Mode;
                Render(dualScores[LeftTeam], mode.GetTeamColor(LeftTeam), 
                       dualScores[RightTeam], mode.GetTeamColor(RightTeam));
            }
            var require = context.sessionContainer.Require<PlayerContainerArrayElement>();
            int leftAlive = 0, rightAlive = 0;
            for (var playerId = 0; playerId < SessionBase.MaxPlayers; playerId++)
            {
                Container player = require[playerId];
                if (player.H().IsActiveAndAlive && player.Require<TeamProperty>().TryWithValue(out byte team))
                {
                    if (team == LeftTeam) leftAlive++;
                    else if (team == RightTeam) rightAlive++;
                }
            }
            for (var i = 0; i < SessionBase.MaxPlayers / 2; i++)
                m_PlayerImages[LeftTeam][i].gameObject.SetActive(i < leftAlive);
            for (var i = 0; i < SessionBase.MaxPlayers / 2; i++)
                m_PlayerImages[RightTeam][i].gameObject.SetActive(i < rightAlive);
            
            SetInterfaceActive(isVisible);
        }
    }
}