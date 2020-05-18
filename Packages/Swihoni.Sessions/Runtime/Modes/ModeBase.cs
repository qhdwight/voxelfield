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

        internal abstract void SpawnPlayer(Container player);

        internal virtual void KillPlayer(Container player)
        {
            player.ZeroIfWith<HealthProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            if (player.With(out StatsComponent stats)) stats.deaths.Value++;
        }

        internal virtual void Modify(Container session, Container playerToModify, Container commands, float duration)
        {
            if (playerToModify.Without(out HealthProperty health) || health.WithoutValue) return;

            if (playerToModify.With(out HitMarkerComponent hitMarker))
                if (hitMarker.elapsed.Value > 0.0f)
                    hitMarker.elapsed.Value -= duration;
            if (playerToModify.With(out DamageNotifierComponent damageNotifier))
                if (damageNotifier.elapsed.Value > 0.0f)
                    damageNotifier.elapsed.Value -= duration;
            if (playerToModify.With(out MoveComponent move) && health.IsAlive && move.position.Value.y < -32.0f)
                KillPlayer(playerToModify);
        }

        public void Modify(Container session, float duration)
        {
            if (session.With(out KillFeedElement killFeed))
            {
                foreach (KillFeedComponent kill in killFeed)
                    if (kill.elapsed > 0.0f)
                        kill.elapsed.Value -= duration;
            }
        }

        public virtual void PlayerHit(SessionBase session, int inflictingPlayerId, PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit, float duration)
        {
            int hitPlayerId = hitbox.Manager.PlayerId;
            Container hitPlayer = session.GetPlayerFromId(hitPlayerId),
                      inflictingPlayer = session.GetPlayerFromId(inflictingPlayerId);
            if (hitPlayer.WithPropertyWithValue(out HealthProperty health) && health.IsAlive && hitPlayer.With<ServerTag>())
            {
                var damage = checked((byte) (weapon.Damage * hitbox.DamageMultiplier));
                InflictDamage(session, inflictingPlayerId, inflictingPlayer, hitPlayer, hitPlayerId, damage);
            }
        }

        public void InflictDamage(SessionBase session, int inflictingPlayerId, Container inflictingPlayer, Container hitPlayer, int hitPlayerId, byte damage)
        {
            checked
            {
                bool usesHitMarker = inflictingPlayer.With(out HitMarkerComponent hitMarker),
                     usesNotifier = hitPlayer.With(out DamageNotifierComponent damageNotifier);
                const float notifierDuration = 1.0f;
                if (usesHitMarker) hitMarker.elapsed.Value = notifierDuration;
                if (usesNotifier) damageNotifier.elapsed.Value = notifierDuration;
                var health = hitPlayer.Require<HealthProperty>();
                if (damage >= health)
                {
                    KillPlayer(hitPlayer);

                    if (inflictingPlayer.With(out StatsComponent stats))
                        stats.kills.Value++;

                    if (usesHitMarker) hitMarker.isKill.Value = true;

                    if (session.GetLatestSession().Without(out KillFeedElement killFeed)) return;
                    foreach (KillFeedComponent kill in killFeed)
                    {
                        // Find empty kill
                        if (kill.elapsed > Mathf.Epsilon) continue;
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