using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class ScoreboardEntryInterface : ElementInterfaceBase<Container>
    {
        [SerializeField] private BufferedTextGui m_UsernameText = default, m_KillsText = default, m_DamageText = default, m_DeathsText = default, m_PingText = default;

        public override void Render(Container _, Container player)
        {
            bool isVisible = player.Without(out HealthProperty health) || health.WithValue;
            if (isVisible && player.With(out StatsComponent stats))
            {
                m_KillsText.BuildText(builder => builder.Append(stats.kills));
                m_DamageText.BuildText(builder => builder.Append(stats.damage));
                m_DeathsText.BuildText(builder => builder.Append(stats.deaths));
                m_PingText.BuildText(builder => builder.Append(stats.ping));
                m_UsernameText.SetText(player.Require<UsernameProperty>().Builder);
            }
            SetInterfaceActive(isVisible);
        }
    }
}