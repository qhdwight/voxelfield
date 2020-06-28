using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    [CreateAssetMenu(fileName = "Deathmatch", menuName = "Session/Mode/Deathmatch", order = 0)]
    public class DeathmatchMode : ModeBase
    {
        protected override void KillPlayer(Container player)
        {
            base.KillPlayer(player);

            if (player.With(out RespawnTimerProperty respawnTimer))
                respawnTimer.Value = 2_000_000u;
        }

        public override void ModifyPlayer(SessionBase session, Container container, Container player, Container commands, uint durationUs)
        {
            base.ModifyPlayer(session, container, player, commands, durationUs);

            if (commands.Without(out InputFlagProperty inputs) || player.Without(out HealthProperty health) || health.WithoutValue)
                return;

            if (inputs.GetInput(PlayerInput.Suicide) && health.IsAlive)
                KillPlayer(player);
            
            HandleRespawn(session, player, health, durationUs);
        }

        protected virtual void HandleRespawn(SessionBase session, Container player, HealthProperty health, uint durationUs)
        {
            if (health.IsDead && player.With(out RespawnTimerProperty respawn))
            {
                if (respawn.Value > durationUs) respawn.Value -= durationUs;
                else
                {
                    respawn.Value = 0u;
                    SpawnPlayer(session, player);
                }
            }
        }
    }
}