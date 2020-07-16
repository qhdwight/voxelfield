using Swihoni.Components;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    [CreateAssetMenu(fileName = "Deathmatch", menuName = "Session/Mode/Deathmatch", order = 0)]
    public class DeathmatchModeBase : ModeBase
    {
        protected override void KillPlayer(Container player, Container killer)
        {
            base.KillPlayer(player, killer);

            if (player.With(out RespawnTimerProperty respawnTimer)) respawnTimer.Value = ConfigManagerBase.Singleton.respawnDuration;
        }

        public override void ModifyPlayer(SessionBase session, Container container, int playerId, Container player, Container commands, uint durationUs, int tickDelta = 1)
        {
            base.ModifyPlayer(session, container, playerId, player, commands, durationUs, tickDelta);

            if (commands.Without(out InputFlagProperty inputs) || player.Without(out HealthProperty health) || health.WithoutValue) return;

            if (inputs.GetInput(PlayerInput.Suicide) && health.IsAlive) InflictDamage(session, playerId, player, player, playerId, health, "Suicide");
            
            if (tickDelta >= 1) HandleAutoRespawn(session, container, playerId, player, health, commands, durationUs);
        }

        protected virtual void HandleAutoRespawn(SessionBase session, Container container, int playerId, Container player, HealthProperty health, Container commands,
                                                 uint durationUs)
        {
            if (health.IsAlive || player.Without(out RespawnTimerProperty respawn)) return;

            if (respawn.Value > durationUs) respawn.Value -= durationUs;
            else
            {
                respawn.Value = 0u;
                SpawnPlayer(session, container, playerId, player);
            }
        }
    }
}