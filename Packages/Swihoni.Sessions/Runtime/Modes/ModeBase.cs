using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public abstract class ModeBase : ScriptableObject
    {
        public byte id;

        protected virtual void SpawnPlayer(SessionBase session, Container player)
        {
            // TODO:refactor zeroing
            if (player.With(out MoveComponent move))
            {
                move.Zero();
                move.position.Value = session.Injector.GetSpawnPosition();
            }
            player.Require<IdProperty>().Value = 1;
            player.Require<UsernameElement>().SetString("Default");
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health))
                health.Value = 100;
            player.ZeroIfWith<RespawnTimerProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            if (player.With(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Shovel, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.TestingRifle, 2);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Shotgun, 3);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Sniper, 4);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Pistol, 5);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Grenade, 6);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Molotov, 7);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.C4, 8);
            }
        }

        internal virtual void KillPlayer(Container player)
        {
            player.ZeroIfWith<HealthProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            if (player.With(out StatsComponent stats)) stats.deaths.Value++;
        }

        public virtual void Modify(SessionBase session, Container container, Container playerToModify, Container commands, uint durationUs)
        {
            if (playerToModify.Without(out HealthProperty health) || health.WithoutValue) return;

            if (playerToModify.With(out HitMarkerComponent hitMarker))
                if (hitMarker.elapsedUs.Value > durationUs) hitMarker.elapsedUs.Value -= durationUs;
                else hitMarker.elapsedUs.Value = 0u;
            if (playerToModify.With(out DamageNotifierComponent damageNotifier))
                if (damageNotifier.elapsedUs.Value > durationUs) damageNotifier.elapsedUs.Value -= durationUs;
                else damageNotifier.elapsedUs.Value = 0u;
            if (playerToModify.With(out MoveComponent move) && health.IsAlive && move.position.Value.y < -32.0f)
                KillPlayer(playerToModify);
        }

        public void Modify(Container session, uint durationUs)
        {
            if (session.With(out KillFeedElement killFeed))
            {
                foreach (KillFeedComponent kill in killFeed)
                    if (kill.elapsedUs > durationUs) kill.elapsedUs.Value -= durationUs;
                    else kill.elapsedUs.Value = 0u;
            }
        }

        public virtual void PlayerHit(SessionBase session, int inflictingPlayerId, PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit, uint durationUs)
        {
            int hitPlayerId = hitbox.Manager.PlayerId;
            Container hitPlayer = session.GetPlayerFromId(hitPlayerId),
                      inflictingPlayer = session.GetPlayerFromId(inflictingPlayerId);
            if (hitPlayer.WithPropertyWithValue(out HealthProperty health) && health.IsAlive && hitPlayer.With<ServerTag>())
            {
                var damage = checked((byte) CalculateWeaponDamage(session, hitPlayer, inflictingPlayer, hitbox, weapon, hit));
                InflictDamage(session, inflictingPlayerId, inflictingPlayer, hitPlayer, hitPlayerId, damage, weapon.itemName);
            }
        }

        protected virtual float CalculateWeaponDamage(SessionBase session, Container hitPlayer, Container inflictingPlayer, PlayerHitbox hitbox, WeaponModifierBase weapon,
                                                      in RaycastHit hit)
            => weapon.GetDamage(hit.distance) * hitbox.DamageMultiplier;

        public void InflictDamage(SessionBase session, int inflictingPlayerId, Container inflictingPlayer, Container hitPlayer, int hitPlayerId, byte damage, string weaponName)
        {
            checked
            {
                bool usesHitMarker = inflictingPlayer.With(out HitMarkerComponent hitMarker),
                     usesNotifier = hitPlayer.With(out DamageNotifierComponent damageNotifier);
                const uint notifierDuration = 1_000_000u;
                if (usesHitMarker) hitMarker.elapsedUs.Value = notifierDuration;
                if (usesNotifier)
                {
                    damageNotifier.elapsedUs.Value = 2_000_000u;
                    damageNotifier.inflictingPlayerId.Value = (byte) inflictingPlayerId;
                    damageNotifier.damage.Value = damage;
                }
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
                        if (kill.elapsedUs > 0u) continue;
                        // Empty kill found
                        kill.elapsedUs.Value = 2_000_000u;
                        kill.killingPlayerId.Value = (byte) inflictingPlayerId;
                        kill.killedPlayerId.Value = (byte) hitPlayerId;
                        kill.weaponName.SetString(weaponName);
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

        public virtual void SetupNewPlayer(SessionBase session, Container player) { }
    }
}