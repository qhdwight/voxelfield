using Swihoni.Sessions.Config;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    [CreateAssetMenu(fileName = "Deathmatch", menuName = "Session/Mode/Deathmatch", order = 0)]
    public class DeathmatchModeBase : ModeBase
    {
        protected override void KillPlayer(in DamageContext damageContext)
        {
            base.KillPlayer(damageContext);

            if (damageContext.sessionContext.player.With(out RespawnTimerProperty respawnTimer)) respawnTimer.Value = ConfigManagerBase.Active.respawnDuration;
        }

        public override void ModifyPlayer(in SessionContext context)
        {
            base.ModifyPlayer(context);

            if (context.commands.Without(out InputFlagProperty inputs) || context.player.Without(out HealthProperty health) || health.WithoutValue) return;

            if (inputs.GetInput(PlayerInput.Suicide) && health.IsAlive)
                InflictDamage(new DamageContext(context, context.playerId, context.player, health, "Suicide"));

            if (context.tickDelta >= 1) HandleAutoRespawn(context, health);
        }

        protected virtual void HandleAutoRespawn(in SessionContext context, HealthProperty health)
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