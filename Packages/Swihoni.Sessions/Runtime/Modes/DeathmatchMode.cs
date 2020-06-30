using Swihoni.Components;
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

            // TODO:refactor variable
            if (player.With(out RespawnTimerProperty respawnTimer)) respawnTimer.Value = 2_000_000u;
        }

        public override void ModifyPlayer(SessionBase session, Container container, int playerId, Container player, Container commands, uint durationUs)
        {
            base.ModifyPlayer(session, container, playerId, player, commands, durationUs);

            if (commands.Without(out InputFlagProperty inputs) || player.Without(out HealthProperty health) || health.WithoutValue)
                return;

            if (inputs.GetInput(PlayerInput.Suicide) && health.IsAlive)
                KillPlayer(player);

            HandleRespawn(session, container, player, health, durationUs);
        }

        protected virtual void HandleRespawn(SessionBase session, Container container, Container player, HealthProperty health, uint durationUs)
        {
            if (health.IsAlive || player.Without(out RespawnTimerProperty respawn)) return;

            if (respawn.Value > durationUs) respawn.Value -= durationUs;
            else
            {
                respawn.Value = 0u;
                SpawnPlayer(session, player);
            }
        }
    }
}