using Swihoni.Components;
using Swihoni.Sessions.Config;
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

            if (player.With(out RespawnTimerProperty respawnTimer)) respawnTimer.Value = ConfigManagerBase.Active.respawnDuration;
        }

        public override void ModifyPlayer(in ModifyContext context)
        {
            base.ModifyPlayer(context);

            if (context.commands.Without(out InputFlagProperty inputs) || context.player.Without(out HealthProperty health) || health.WithoutValue) return;

            if (inputs.GetInput(PlayerInput.Suicide) && health.IsAlive)
                InflictDamage(new DamageContext(context, context.playerId, context.player, context.player, context.playerId, health, "Suicide"));

            if (context.tickDelta >= 1) HandleAutoRespawn(context, health);
        }

        protected virtual void HandleAutoRespawn(in ModifyContext context, HealthProperty health)
        {
            if (health.IsAlive || context.player.Without(out RespawnTimerProperty respawn)) return;

            if (respawn.Value > context.durationUs) respawn.Value -= context.durationUs;
            else
            {
                respawn.Value = 0u;
                SpawnPlayer(context);
            }
        }
    }
}