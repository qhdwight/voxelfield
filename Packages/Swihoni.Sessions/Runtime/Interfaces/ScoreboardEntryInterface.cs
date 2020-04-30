using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Interfaces
{
    public class ScoreboardEntryInterface : InterfaceBehaviorBase
    {
        [SerializeField] private BufferedTextGui m_UsernameText = default, m_KillsText = default, m_DamageText = default, m_DeathsText = default, m_PingText = default;

        public void Render(Container player)
        {
            bool isVisible = player.Without(out HealthProperty health) || health.HasValue;
            if (isVisible && player.Has(out StatsComponent stats))
            {
                m_KillsText.Set(builder => builder.Append(stats.kills));
                m_DamageText.Set(builder => builder.Append(stats.damage));
                m_DeathsText.Set(builder => builder.Append(stats.deaths));
                m_PingText.Set(builder => builder.Append(stats.ping));
            }
            SetInterfaceActive(isVisible);
        }
    }
}