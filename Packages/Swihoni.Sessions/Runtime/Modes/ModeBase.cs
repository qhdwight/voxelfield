using System;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public abstract class ModeBase : ScriptableObject
    {
        public byte id;

        internal abstract void ResetPlayer(Container player);

        internal virtual void KillPlayer(Container player)
        {
            if (player.Has(out HealthProperty health)) health.Value = 0;
        }

        internal virtual void Modify(Container playerToModify, Container commands, float duration)
        {
            if (playerToModify.Without(out HitMarkerComponent hitMarker)) return;

            if (hitMarker.elapsed.Value > 0.0f) hitMarker.elapsed.Value -= duration;
        }

        public void ModifyChecked(ModeBase mode, Container containerToModify, Container commands, float duration) { }

        public void ModifyTrusted(ModeBase mode, Container containerToModify, Container commands, float duration) { }

        public void ModifyCommands(ModeBase mode, Container commandsToModify) { throw new NotImplementedException(); }

        public virtual void PlayerHit(SessionBase session, int inflictingPlayerId, PlayerHitbox hitbox, GunModifierBase gun, in RaycastHit hit, float duration)
        {
            Container hitPlayer = session.GetPlayerFromId(hitbox.Manager.PlayerId),
                      inflictingPlayer = session.GetPlayerFromId(inflictingPlayerId);
            if (hitPlayer.Present(out HealthProperty health) && health.IsAlive)
            {
                if (hitPlayer.Has<ServerTag>())
                {
                    bool usesHitMarker = inflictingPlayer.Has(out HitMarkerComponent hitMarker);
                    if (usesHitMarker) hitMarker.elapsed.Value = 1.0f;
                    checked
                    {
                        var damage = (byte) (gun.Damage * hitbox.DamageMultiplier);
                        if (damage >= health)
                        {
                            KillPlayer(hitPlayer);
                            if (inflictingPlayer.Has(out StatsComponent stats))
                                stats.kills.Value++;
                            if (usesHitMarker) hitMarker.isKill.Value = true;
                        }
                        else
                        {
                            health.Value -= damage;
                            if (usesHitMarker) hitMarker.isKill.Value = false;
                        }
                    }
                }
            }
        }
    }
}