using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    [CreateAssetMenu(fileName = "Deathmatch", menuName = "Session/Mode/Deathmatch", order = 0)]
    public class DeathmatchMode : ModeBase
    {
        internal override void KillPlayer(Container player)
        {
            base.KillPlayer(player);

            if (player.With(out RespawnTimerProperty respawnTimer))
                respawnTimer.Value = 2_000_000u;
        }

        internal override void Modify(SessionBase session, Container container, Container playerToModify, Container commands, uint durationUs)
        {
            base.Modify(session, container, playerToModify, commands, durationUs);

            if (commands.Without(out InputFlagProperty inputs) || playerToModify.Without(out HealthProperty health) || health.WithoutValue)
                return;

            if (inputs.GetInput(PlayerInput.Suicide) && health.IsAlive)
                KillPlayer(playerToModify);
            if (health.IsDead && playerToModify.With(out RespawnTimerProperty respawn))
            {
                if (respawn.Value > durationUs) respawn.Value -= durationUs;
                else
                {
                    respawn.Value = 0u;
                    SpawnPlayer(session, playerToModify);
                }
            }
        }
    }
}