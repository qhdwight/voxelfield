using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class ScoreboardEntryInterface : ElementInterfaceBase<Container>
    {
        [SerializeField] private BufferedTextGui m_UsernameText = default, m_KillsText = default, m_DamageText = default, m_DeathsText = default, m_PingText = default;

        public override void Render(Container player)
        {
            bool isVisible = player.Without(out HealthProperty health) || health.HasValue;
            if (isVisible && player.Has(out StatsComponent stats))
            {
                m_KillsText.SetText(builder => builder.Append(stats.kills));
                m_DamageText.SetText(builder => builder.Append(stats.damage));
                m_DeathsText.SetText(builder => builder.Append(stats.deaths));
                m_PingText.SetText(builder => builder.Append(stats.ping));
            }
            SetInterfaceActive(isVisible);
        }
    }
}