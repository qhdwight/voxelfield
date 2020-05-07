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
            if (player.Has(out StatsComponent stats)) stats.deaths.Value++;
        }

        internal virtual void Modify(Container session, Container playerToModify, Container commands, float duration)
        {
            if (playerToModify.Has(out HitMarkerComponent hitMarker))
                if (hitMarker.elapsed.Value > 0.0f) hitMarker.elapsed.Value -= duration;
            if (playerToModify.Has(out DamageNotifierComponent damageNotifier))
                if (damageNotifier.elapsed.Value > 0.0f) damageNotifier.elapsed.Value -= duration;
            if (playerToModify.Has(out HealthProperty health) && health.IsAlive && playerToModify.Has(out MoveComponent move) && move.position.Value.y < -5.0f)
            {
                KillPlayer(playerToModify);
            }
            if (session.Has(out KillFeedProperty killFeed))
            {
                foreach (KillFeedComponent kill in killFeed)
                    if (kill.elapsed > 0.0f)
                        kill.elapsed.Value -= duration;
            }
        }

        public void ModifyChecked(ModeBase mode, Container containerToModify, Container commands, float duration) { }

        public void ModifyTrusted(ModeBase mode, Container containerToModify, Container commands, float duration) { }

        public void ModifyCommands(ModeBase mode, Container commandsToModify) { throw new NotImplementedException(); }

        public virtual void PlayerHit(SessionBase session, int inflictingPlayerId, PlayerHitbox hitbox, GunModifierBase gun, in RaycastHit hit, float duration)
        {
            int hitPlayerId = hitbox.Manager.PlayerId;
            Container hitPlayer = session.GetPlayerFromId(hitPlayerId),
                      inflictingPlayer = session.GetPlayerFromId(inflictingPlayerId);
            if (hitPlayer.Present(out HealthProperty health) && health.IsAlive && hitPlayer.Has<ServerTag>())
            {
                var damage = checked((byte) (gun.Damage * hitbox.DamageMultiplier));
                InflictDamage(session, inflictingPlayerId, inflictingPlayer, hitPlayer, hitPlayerId, damage);
            }
        }

        public void InflictDamage(SessionBase session, int inflictingPlayerId, Container inflictingPlayer, Container hitPlayer, int hitPlayerId, byte damage)
        {
            checked
            {
                bool usesHitMarker = inflictingPlayer.Has(out HitMarkerComponent hitMarker),
                     usesNotifier = hitPlayer.Has(out DamageNotifierComponent damageNotifier);
                if (usesHitMarker) hitMarker.elapsed.Value = 1.0f;
                if (usesNotifier) damageNotifier.elapsed.Value = 1.0f;
                var health = hitPlayer.Require<HealthProperty>();
                if (damage >= health)
                {
                    KillPlayer(hitPlayer);

                    if (inflictingPlayer.Has(out StatsComponent stats))
                        stats.kills.Value++;

                    if (usesHitMarker) hitMarker.isKill.Value = true;

                    if (session.GetLatestSession().Without(out KillFeedProperty killFeed)) return;
                    foreach (KillFeedComponent kill in killFeed)
                    {
                        // Find empty kill
                        if (kill.elapsed > float.Epsilon) continue;
                        kill.elapsed.Value = 2.0f;
                        kill.killedPlayerId.Value = (byte) hitPlayerId;
                        kill.killingPlayerId.Value = (byte) inflictingPlayerId;
                        break;
                    }
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