using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
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

        internal override void Modify(SessionBase sesh, Container session, Container playerToModify, Container commands, uint durationUs)
        {
            base.Modify(sesh, session, playerToModify, commands, durationUs);

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
                    SpawnPlayer(sesh, playerToModify);
                }
            }
        }
    }
}