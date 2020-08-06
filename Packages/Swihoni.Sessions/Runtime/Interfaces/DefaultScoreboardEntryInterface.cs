using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class DefaultScoreboardEntryInterface : ElementInterfaceBase<Container>
    {
        [SerializeField] protected BufferedTextGui m_UsernameText = default, m_KillsText = default, m_DamageText = default, m_DeathsText = default, m_PingText = default;

        public override void Render(in SessionContext context, Container player)
        {
            bool isVisible = player.Without(out HealthProperty health) || health.WithValue;
            if (isVisible && player.With(out StatsComponent stats))
            {
                m_KillsText.StartBuild().Append(stats.kills).Commit(m_KillsText);
                m_DamageText.StartBuild().Append(stats.damage).Commit(m_DamageText);
                m_DeathsText.StartBuild().Append(stats.deaths).Commit(m_DeathsText);
                m_PingText.StartBuild().Append(stats.ping).Commit(m_PingText);
                context.Mode.BuildUsername(m_UsernameText.StartBuild(), player).Commit(m_UsernameText);
            }
            SetInterfaceActive(isVisible);
        }
    }
}