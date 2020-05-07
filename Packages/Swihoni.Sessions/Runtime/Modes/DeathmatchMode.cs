using Swihoni.Components;
using Swihoni.Sessions.Components;
using Swihoni.Sessions.Items.Modifiers;
using Swihoni.Sessions.Player.Components;
using Swihoni.Sessions.Player.Modifiers;
using UnityEngine;

namespace Swihoni.Sessions.Modes
{
    [CreateAssetMenu(fileName = "Deathmatch", menuName = "Session/Mode/Deathmatch", order = 0)]
    public class DeathmatchMode : ModeBase
    {
        internal override void ResetPlayer(Container player)
        {
            // TODO:refactor zeroing
            if (player.Has(out MoveComponent move))
            {
                move.Zero();
                move.position.Value = new Vector3 {y = 10.0f};
            }
            player.ZeroIfHas<CameraComponent>();
            if (player.Has(out HealthProperty health))
                health.Value = 100;
            player.ZeroIfHas<RespawnTimerProperty>();
            player.ZeroIfHas<HitMarkerComponent>();
            player.ZeroIfHas<DamageNotifierComponent>();
            if (player.Has(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Grenade, 2);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Molotov, 3);
            }
        }

        internal override void KillPlayer(Container player)
        {
            base.KillPlayer(player);

            if (player.Has(out RespawnTimerProperty respawnTimer))
                respawnTimer.Value = 2.0f;
        }

        internal override void Modify(Container session, Container playerToModify, Container commands, float duration)
        {
            base.Modify(session, playerToModify, commands, duration);

            if (commands.Without(out InputFlagProperty inputs)
             || playerToModify.Without<ServerTag>()
             || playerToModify.Without(out HealthProperty health)) return;

            if (inputs.GetInput(PlayerInput.Suicide) && health.IsAlive)
            {
                KillPlayer(playerToModify);
            }
            if (health.IsDead && playerToModify.Has(out RespawnTimerProperty respawn))
            {
                respawn.Value -= duration;
                if (respawn.Value < 0.0f)
                {
                    ResetPlayer(playerToModify);
                }
            }
        }
    }
}