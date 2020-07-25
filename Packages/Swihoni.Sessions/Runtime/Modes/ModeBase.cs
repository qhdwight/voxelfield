using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public readonly struct DamageContext
    {
        public readonly ModifyContext modifyContext;
        public readonly bool isHeadShot;
        public readonly string weaponName;
        public readonly byte damage;
        public readonly int hitPlayerId;
        public readonly Container hitPlayer;

        public int InflictingPlayerId => modifyContext.playerId;
        public Container InflictingPlayer => modifyContext.player;

        public bool IsSelfInflicting => hitPlayerId == InflictingPlayerId;

        public DamageContext(in ModifyContext? modifyContext = null,
                             int? hitPlayerId = null,
                             Container hitPlayer = null,
                             byte? damage = null, string weaponName = null, bool? isHeadShot = null, PlayerHitContext? playerHitContext = null)
        {
            if (playerHitContext is PlayerHitContext context)
            {
                this.modifyContext = context.modifyContext;
                this.hitPlayerId = context.hitPlayerId;
                this.hitPlayer = context.hitPlayer;
                this.damage = damage.GetValueOrDefault();
                this.weaponName = weaponName;
                this.isHeadShot = isHeadShot.GetValueOrDefault();
            }
            else
            {
                this.modifyContext = modifyContext.GetValueOrDefault();
                this.hitPlayerId = hitPlayerId.GetValueOrDefault();
                this.hitPlayer = hitPlayer;
                this.damage = damage.GetValueOrDefault();
                this.weaponName = weaponName;
                this.isHeadShot = isHeadShot.GetValueOrDefault();
            }
        }
    }

    public readonly struct PlayerHitContext
    {
        public readonly ModifyContext modifyContext;
        public readonly int hitPlayerId;
        public readonly Container hitPlayer;
        public readonly PlayerHitbox hitbox;
        public readonly WeaponModifierBase weapon;
        public readonly RaycastHit hit;

        public PlayerHitContext(in ModifyContext modifyContext, PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit)
        {
            this.modifyContext = modifyContext;
            this.hitbox = hitbox;
            this.weapon = weapon;
            this.hit = hit;
            hitPlayerId = hitbox.Manager.PlayerId;
            hitPlayer = modifyContext.GetModifyingPlayer(hitPlayerId);
        }
    }

    public abstract class ModeBase : ScriptableObject
    {
        private static readonly Dictionary<Color, string> CachedHex = new Dictionary<Color, string>();

        public byte id;

        public static string GetHexColor(in Color color)
        {
            if (CachedHex.TryGetValue(color, out string hex))
                return hex;
            hex = ColorUtility.ToHtmlStringRGB(color);
            CachedHex.Add(color, hex);
            return hex;
        }

        protected virtual void SpawnPlayer(in ModifyContext context)
        {
            // TODO:refactor zeroing
            Container player = context.player;
            player.ZeroIfWith<FrozenProperty>();
            player.Require<IdProperty>().Value = 1;
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health)) health.Value = ConfigManagerBase.Active.respawnHealth;
            player.ZeroIfWith<RespawnTimerProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            if (player.With(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.AddItems(inventory, ItemId.Pickaxe,
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
                move.position.Value = GetSpawnPosition(context);
            }
        }

        public virtual bool CanSpectate(Container session, Container player) => false;

        public virtual Color GetTeamColor(int teamId) => Color.white;

        protected virtual Vector3 GetSpawnPosition(in ModifyContext context) => new Vector3 {y = 8.0f};

        public virtual void BeginModify(in ModifyContext context)
            => ForEachActivePlayer(context, (in ModifyContext playerModifyContext) => SpawnPlayer(playerModifyContext));

        protected virtual void KillPlayer(in DamageContext context)
        {
            SessionBase session = context.modifyContext.session;
            session.Injector.OnKillPlayer(context);
            Container player = context.modifyContext.player;
            player.ZeroIfWith<HealthProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            if (player.With(out StatsComponent stats)) stats.deaths.Value++;
        }

        public virtual void Render(SessionBase session, Container sessionContainer) => session.Injector.OnRenderMode(sessionContainer);

        public virtual void ModifyPlayer(in ModifyContext context)
        {
            if (context.player.Without(out HealthProperty health) || health.WithoutValue) return;

            if (context.tickDelta >= 1)
            {
                if (context.player.With(out HitMarkerComponent hitMarker))
                    if (hitMarker.elapsedUs.Value > context.durationUs) hitMarker.elapsedUs.Value -= context.durationUs;
                    else hitMarker.elapsedUs.Value = 0u;
                if (context.player.With(out DamageNotifierComponent damageNotifier))
                    if (damageNotifier.elapsedUs.Value > context.durationUs) damageNotifier.elapsedUs.Value -= context.durationUs;
                    else damageNotifier.elapsedUs.Value = 0u;
                if (context.player.With(out MoveComponent move) && health.IsAlive && move.position.Value.y < -32.0f)
                    InflictDamage(new DamageContext(context, context.playerId, context.player, health.Value, "Void"));
            }

            if (context.commands.WithPropertyWithValue(out WantedTeamProperty wantedTeam) && wantedTeam != context.player.Require<TeamProperty>() && AllowTeamSwap(context))
            {
                context.player.Require<TeamProperty>().Value = wantedTeam;
                SpawnPlayer(context);
            }
        }

        public virtual bool AllowTeamSwap(in ModifyContext context) => true;

        public virtual void Modify(in ModifyContext context)
        {
            BoolProperty restartMode = ConfigManagerBase.Active.restartMode;
            if (restartMode)
            {
                EndModify(context);
                BeginModify(context);
                restartMode.Value = false;
            }

            if (context.sessionContainer.With(out KillFeedElement killFeed))
            {
                foreach (KillFeedComponent kill in killFeed)
                    if (kill.elapsedUs > context.durationUs) kill.elapsedUs.Value -= context.durationUs;
                    else kill.elapsedUs.Value = 0u;
            }
        }

        public virtual void PlayerHit(in PlayerHitContext playerHitContext)
        {
            if (playerHitContext.hitPlayer.WithPropertyWithValue(out HealthProperty health) && health.IsAlive && playerHitContext.hitPlayer.With<ServerTag>())
            {
                var damage = checked((byte) Mathf.Clamp(CalculateWeaponDamage(playerHitContext), 0.0f, 255.0f));
                var damageContext = new DamageContext(damage: damage, weaponName: playerHitContext.weapon.itemName, isHeadShot: playerHitContext.hitbox.IsHead,
                                                      playerHitContext: playerHitContext);
                InflictDamage(damageContext);
            }
        }

        protected virtual float CalculateWeaponDamage(in PlayerHitContext context)
        {
            float damage = context.weapon.GetDamage(context.hit.distance) * context.hitbox.DamageMultiplier;
            if (context.hitbox.IsHead) damage *= context.weapon.HeadShotMultiplier;
            return damage;
        }

        public void InflictDamage(in DamageContext damageContext)
        {
            checked
            {
                bool isSelfInflicting = damageContext.IsSelfInflicting,
                     usesHitMarker = damageContext.InflictingPlayer.With(out HitMarkerComponent hitMarker) && !isSelfInflicting,
                     usesNotifier = damageContext.hitPlayer.With(out DamageNotifierComponent damageNotifier);

                var health = damageContext.hitPlayer.Require<HealthProperty>();
                bool isKilling = damageContext.damage >= health;
                if (isKilling)
                {
                    KillPlayer(damageContext);

                    if (!isSelfInflicting && damageContext.InflictingPlayer.With(out StatsComponent stats))
                        stats.kills.Value++;

                    if (usesHitMarker) hitMarker.isKill.Value = true;

                    if (damageContext.modifyContext.sessionContainer.Without(out KillFeedElement killFeed)) return;
                    foreach (KillFeedComponent kill in killFeed)
                    {
                        if (kill.elapsedUs > 0u) continue;
                        // Empty kill found
                        kill.elapsedUs.Value = 2_000_000u;
                        kill.killingPlayerId.Value = (byte) damageContext.InflictingPlayerId;
                        kill.killedPlayerId.Value = (byte) damageContext.hitPlayerId;
                        kill.isHeadShot.Value = damageContext.isHeadShot;
                        kill.weaponName.SetTo(damageContext.weaponName);
                        break;
                    }

                    if (isSelfInflicting) usesNotifier = false;
                }
                else
                {
                    health.Value -= damageContext.damage;
                    if (usesHitMarker) hitMarker.isKill.Value = false;
                }

                const uint notifierDuration = 1_000_000u;
                if (usesHitMarker) hitMarker.elapsedUs.Value = notifierDuration;
                if (usesNotifier)
                {
                    damageNotifier.elapsedUs.Value = 2_000_000u;
                    damageNotifier.inflictingPlayerId.Value = (byte) damageContext.InflictingPlayerId;
                    damageNotifier.damage.Value = damageContext.damage;
                }
            }
        }

        public virtual void SetupNewPlayer(in ModifyContext context) => SpawnPlayer(context);

        public delegate void ModifyPlayerAction(in ModifyContext playerModifyContext);

        protected static void ForEachActivePlayer(in ModifyContext context, ModifyPlayerAction action)
        {
            for (var playerId = 0; playerId < SessionBase.MaxPlayers; playerId++)
            {
                Container player = context.GetModifyingPlayer(playerId);
                if (player.Require<HealthProperty>().WithValue)
                {
                    var playerModifyContext = new ModifyContext(existing: context, player: player, playerId: playerId);
                    action(playerModifyContext);
                }
            }
        }

        public virtual void EndModify(in ModifyContext context) { }

        // public virtual bool RestrictMovement(Vector3 prePosition, Vector3 postPosition) => false;

        public virtual StringBuilder BuildUsername(StringBuilder builder, Container player)
            => builder.AppendPropertyValue(player.Require<UsernameProperty>());

        protected static int GetActivePlayerCount(Container session)
            => session.Require<PlayerContainerArrayElement>().Count(player => player.Require<HealthProperty>().WithValue);

        public virtual void Initialize() { }
    }
}