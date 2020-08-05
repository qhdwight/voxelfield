using System.Collections.Generic;
using System.Linq;
using System.Text;
using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Config;
using Swihoni.Sessions.Entities;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    public readonly struct DamageContext
    {
        public readonly SessionContext sessionContext;
        public readonly bool isHeadShot;
        public readonly string weaponName;
        public readonly byte damage;
        public readonly int hitPlayerId;
        public readonly Container hitPlayer;

        public int InflictingPlayerId => sessionContext.playerId;
        public Container InflictingPlayer => sessionContext.player;

        public bool IsSelfInflicting => hitPlayerId == InflictingPlayerId;

        public DamageContext(in SessionContext? modifyContext = null,
                             int? hitPlayerId = null,
                             Container hitPlayer = null,
                             byte? damage = null, string weaponName = null, bool? isHeadShot = null, PlayerHitContext? playerHitContext = null)
        {
            if (playerHitContext is PlayerHitContext context)
            {
                this.sessionContext = context.sessionContext;
                this.hitPlayerId = context.hitPlayerId;
                this.hitPlayer = context.hitPlayer;
                this.damage = damage.GetValueOrDefault();
                this.weaponName = weaponName;
                this.isHeadShot = isHeadShot.GetValueOrDefault();
            }
            else
            {
                this.sessionContext = modifyContext.GetValueOrDefault();
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
        public readonly SessionContext sessionContext;
        public readonly int hitPlayerId;
        public readonly Container hitPlayer;
        public readonly PlayerHitbox hitbox;
        public readonly WeaponModifierBase weapon;
        public readonly RaycastHit hit;

        public PlayerHitContext(in SessionContext sessionContext, PlayerHitbox hitbox, WeaponModifierBase weapon, in RaycastHit hit)
        {
            this.sessionContext = sessionContext;
            this.hitbox = hitbox;
            this.weapon = weapon;
            this.hit = hit;
            hitPlayerId = hitbox.Manager.PlayerId;
            hitPlayer = sessionContext.GetModifyingPlayer(hitPlayerId);
        }
    }

    public abstract class ModeBase : ScriptableObject
    {
        private static readonly Dictionary<Color, string> CachedHex = new Dictionary<Color, string>();

        [RuntimeInitializeOnLoadMethod]
        private static void InitializeCommands() => SessionBase.RegisterSessionCommand("restart_mode", "force_start");

        public byte id;

        public static string GetHexColor(in Color color)
        {
            if (CachedHex.TryGetValue(color, out string hex))
                return hex;
            hex = ColorUtility.ToHtmlStringRGB(color);
            CachedHex.Add(color, hex);
            return hex;
        }

        protected virtual void SpawnPlayer(in SessionContext context, bool begin = false)
        {
            Container player = context.player;
            player.ZeroIfWith<FrozenProperty>();
            if (begin) player.ZeroIfWith<StatsComponent>();
            player.Require<ByteIdProperty>().Value = 1;
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health)) health.Value = ConfigManagerBase.Active.respawnHealth;
            player.ZeroIfWith<RespawnTimerProperty>();
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

        protected virtual Vector3 GetSpawnPosition(in SessionContext context) => new Vector3 {y = 8.0f};

        public virtual void BeginModify(in SessionContext context)
        {
            Navigation ClearModeElements(ElementBase _element)
            {
                if (_element.WithAttribute<ModeElementAttribute>())
                {
                    _element.Clear();
                    return Navigation.SkipDescendents;
                }
                return Navigation.Continue;
            }
            Container session = context.sessionContainer;
            session.Navigate(_element => _element is PlayerContainerArrayElement ? Navigation.SkipDescendents : ClearModeElements(_element));
            context.ForEachActivePlayer((in SessionContext playerModifyContext) =>
            {
                playerModifyContext.player.Navigate(ClearModeElements);
                SpawnPlayer(playerModifyContext, true);
            });
        }

        protected virtual void KillPlayer(in DamageContext context)
        {
            context.sessionContext.session.Injector.OnKillPlayer(context);
            Container player = context.hitPlayer;
            player.ZeroIfWith<HealthProperty>();
            player.ClearIfWith<HitMarkerComponent>();
            if (player.With(out StatsComponent stats)) stats.deaths.Value++;
            if (player.With(out InventoryComponent inventory))
            {
                for (var i = 1; i <= inventory.Count; i++)
                {
                    ItemComponent item = inventory[i];
                    item.status.Zero();
                }
                inventory.equipStatus.id.Value = ItemEquipStatusId.Equipping;
                inventory.equipStatus.elapsedUs.Value = 0u;
            }
        }

        public virtual void Render(SessionBase session, Container sessionContainer) => session.Injector.OnRenderMode(sessionContainer);

        public virtual void ModifyPlayer(in SessionContext context)
        {
            if (context.player.Without(out HealthProperty health) || health.WithoutValue) return;

            if (context.tickDelta >= 1)
            {
                if (context.player.With(out HitMarkerComponent hitMarker) && hitMarker.elapsedUs.WithValue)
                    if (hitMarker.elapsedUs.Value > context.durationUs) hitMarker.elapsedUs.Value -= context.durationUs;
                    else hitMarker.Clear();
                if (context.player.With(out DamageNotifierComponent damageNotifier) && damageNotifier.elapsedUs.WithValue)
                    if (damageNotifier.elapsedUs.Value > context.durationUs) damageNotifier.elapsedUs.Value -= context.durationUs;
                    else damageNotifier.Clear();
                if (context.player.With(out MoveComponent move) && health.IsAlive && move.position.Value.y < -32.0f)
                    InflictDamage(new DamageContext(context, context.playerId, context.player, health.Value, "Void"));
            }

            if (context.commands.WithPropertyWithValue(out WantedTeamProperty wantedTeam) && wantedTeam != context.player.Require<TeamProperty>() && AllowTeamSwap(context))
            {
                context.player.Require<TeamProperty>().Value = wantedTeam;
                SpawnPlayer(context);
            }

            if (!PlayerModifierBehaviorBase.WithServerStringCommands(context, out IEnumerable<string[]> commands)) return;
            foreach (string[] arguments in commands)
                if (arguments[0] == "restart_mode")
                {
                    EndModify(context);
                    BeginModify(context);
                }
        }

        public virtual bool AllowTeamSwap(in SessionContext context) => true;

        public virtual void Modify(in SessionContext context)
        {
            if (context.sessionContainer.Without(out KillFeedElement killFeed)) return;
            foreach (KillFeedComponent kill in killFeed)
                if (kill.elapsedUs.WithValue)
                    if (kill.elapsedUs > context.durationUs) kill.elapsedUs.Value -= context.durationUs;
                    else kill.Clear();
        }

        public virtual void PlayerHit(in PlayerHitContext playerHitContext)
        {
            if (playerHitContext.hitPlayer.WithPropertyWithValue(out HealthProperty health) && health.IsAlive && playerHitContext.hitPlayer.With<ServerTag>())
            {
                var damage = checked((byte) Mathf.Clamp(CalculateWeaponDamage(playerHitContext), 0.0f, 200.0f));
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

                    if (damageContext.sessionContext.sessionContainer.Without(out KillFeedElement killFeed)) return;
                    foreach (KillFeedComponent kill in killFeed)
                    {
                        if (kill.elapsedUs.WithValue) continue;
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

        public virtual void SetupNewPlayer(in SessionContext context) => SpawnPlayer(context, true);

        public virtual void EndModify(in SessionContext context) { }

        // public virtual bool RestrictMovement(Vector3 prePosition, Vector3 postPosition) => false;

        public virtual StringBuilder BuildUsername(StringBuilder builder, Container player)
        {
            Color color = GetTeamColor(player.Require<TeamProperty>());
            string hex = GetHexColor(color);
            return builder.Append("<color=#").Append(hex).Append(">").AppendPropertyValue(player.Require<UsernameProperty>()).Append("</color>");
        }

        public virtual Color GetTeamColor(byte? teamId) => Color.white;
        public Color GetTeamColor(Container player) => GetTeamColor(player.Require<TeamProperty>());
        public virtual Color GetTeamColor(TeamProperty team) => GetTeamColor(team.AsNullable);

        protected static int GetActivePlayerCount(Container session)
            => session.Require<PlayerContainerArrayElement>().Count(player => player.Require<HealthProperty>().WithValue);

        public virtual void Initialize() { }

        public virtual uint ItemEntityLifespanUs => 20_000_000u;
    }
}