using System;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using Swihoni.Util.Interface;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public abstract class ModeBase : ScriptableObject
    {
        public byte id;

        protected virtual void SpawnPlayer(SessionBase session, Container sessionContainer, int playerId, Container player)
        {
            Debug.Log("Spawning player");
            // TODO:refactor zeroing
            player.ZeroIfWith<FrozenProperty>();
            player.Require<IdProperty>().Value = 1;
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health)) health.Value = 100;
            player.ZeroIfWith<RespawnTimerProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            if (player.With(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.AddItems(inventory, ItemId.Shovel,
                                                          ItemId.Rifle,
                                                          ItemId.Shotgun,
                                                          ItemId.Sniper,
                                                          ItemId.Deagle,
                                                          ItemId.Grenade,
                                                          ItemId.Molotov,
                                                          ItemId.C4,
                                                          ItemId.Smg,
                                                          ItemId.MissileLauncher);
            }
            if (player.With(out MoveComponent move))
            {
                move.Zero();
                move.position.Value = session.GetMode().GetSpawnPosition(player, playerId, session, sessionContainer);
            }
        }

        protected virtual Vector3 GetSpawnPosition(Container player, int playerId, SessionBase session, Container sessionContainer) => new Vector3 {y = 8.0f};

        public virtual void Begin(SessionBase session, Container sessionContainer)
        {
            if (!session.IsPaused)
                ForEachActivePlayer(session, sessionContainer, (playerId, player) => SpawnPlayer(session, sessionContainer, playerId, player));
        }

        protected virtual void KillPlayer(Container player)
        {
            player.ZeroIfWith<HealthProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            if (player.With(out StatsComponent stats)) stats.deaths.Value++;
        }

        public virtual void Render(SessionBase session, Container sessionContainer) => session.Injector.OnRenderMode(sessionContainer);

        public virtual void ModifyPlayer(SessionBase session, Container container, int playerId, Container player, Container commands, uint durationUs, int tickDelta = 1)
        {
            if (player.Without(out HealthProperty health) || health.WithoutValue) return;

            if (player.With(out HitMarkerComponent hitMarker))
                if (hitMarker.elapsedUs.Value > durationUs) hitMarker.elapsedUs.Value -= durationUs;
                else hitMarker.elapsedUs.Value = 0u;
            if (player.With(out DamageNotifierComponent damageNotifier))
                if (damageNotifier.elapsedUs.Value > durationUs) damageNotifier.elapsedUs.Value -= durationUs;
                else damageNotifier.elapsedUs.Value = 0u;
            if (player.With(out MoveComponent move) && health.IsAlive && move.position.Value.y < -32.0f)
                InflictDamage(session, playerId, player, player, playerId, health.Value, "Void");

            if (commands.With(out WantedTeamProperty wantedTeam) && AllowTeamSwap(container, player))
            {
                player.Require<TeamProperty>().Value = wantedTeam;
            }
        }

        public virtual bool AllowTeamSwap(Container container, Container player) => true;

        public virtual void Modify(SessionBase session, Container container, uint durationUs)
        {
            if (container.With(out KillFeedElement killFeed))
            {
                foreach (KillFeedComponent kill in killFeed)
                    if (kill.elapsedUs > durationUs) kill.elapsedUs.Value -= durationUs;
                    else kill.elapsedUs.Value = 0u;
            }
        }

        public virtual void PlayerHit(SessionBase session, int inflictingPlayerId, PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit, uint durationUs)
        {
            int hitPlayerId = hitbox.Manager.PlayerId;
            Container hitPlayer = session.GetModifyingPayerFromId(hitPlayerId),
                      inflictingPlayer = session.GetModifyingPayerFromId(inflictingPlayerId);
            if (hitPlayer.WithPropertyWithValue(out HealthProperty health) && health.IsAlive && hitPlayer.With<ServerTag>())
            {
                var damage = checked((byte) Mathf.Clamp(CalculateWeaponDamage(session, hitPlayer, inflictingPlayer, hitbox, weapon, hit), 0.0f, 255.0f));
                InflictDamage(session, inflictingPlayerId, inflictingPlayer, hitPlayer, hitPlayerId, damage, weapon.itemName, hitbox.IsHead);
            }
        }

        protected virtual float CalculateWeaponDamage(SessionBase session, Container hitPlayer, Container inflictingPlayer,
                                                      PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit)
        {
            float damage = weapon.GetDamage(hit.distance) * hitbox.DamageMultiplier;
            if (hitbox.IsHead) damage *= weapon.HeadShotMultiplier;
            return damage;
        }

        public void InflictDamage(SessionBase session, int inflictingPlayerId, Container inflictingPlayer, Container hitPlayer, int hitPlayerId, byte damage, string weaponName,
                                  bool isHeadShot = false)
        {
            checked
            {
                bool isSelfInflicting = inflictingPlayerId == hitPlayerId,
                     usesHitMarker = inflictingPlayer.With(out HitMarkerComponent hitMarker) && !isSelfInflicting,
                     usesNotifier = hitPlayer.With(out DamageNotifierComponent damageNotifier);

                var health = hitPlayer.Require<HealthProperty>();
                bool isKilling = damage >= health;
                if (isKilling)
                {
                    KillPlayer(hitPlayer);

                    if (!isSelfInflicting && inflictingPlayer.With(out StatsComponent stats))
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
                        kill.isHeadShot.Value = isHeadShot;
                        kill.weaponName.SetTo(weaponName);
                        break;
                    }

                    if (isSelfInflicting) usesNotifier = false;
                }
                else
                {
                    health.Value -= damage;
                    if (usesHitMarker) hitMarker.isKill.Value = false;
                }

                const uint notifierDuration = 1_000_000u;
                if (usesHitMarker) hitMarker.elapsedUs.Value = notifierDuration;
                if (usesNotifier)
                {
                    damageNotifier.elapsedUs.Value = 2_000_000u;
                    damageNotifier.inflictingPlayerId.Value = (byte) inflictingPlayerId;
                    damageNotifier.damage.Value = damage;
                }
            }
        }

        public virtual void SetupNewPlayer(SessionBase session, int playerId, Container player, Container sessionContainer, Container sessionContainer1) =>
            SpawnPlayer(session, sessionContainer, playerId, player);

        protected static void ForEachActivePlayer(SessionBase session, Container container, Action<int, Container> action)
        {
            for (var playerId = 0; playerId < SessionBase.MaxPlayers; playerId++)
            {
                Container player = session.GetModifyingPayerFromId(playerId, container);
                if (player.Require<HealthProperty>().WithValue)
                    action(playerId, player);
            }
        }

        public virtual void End() { }

        // public virtual bool RestrictMovement(Vector3 prePosition, Vector3 postPosition) => false;

        public virtual StringBuilder BuildUsername(StringBuilder builder, Container player)
            => builder.Append(player.Require<UsernameProperty>().Builder);
    }
}