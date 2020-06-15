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
        internal override void SpawnPlayer(Container player)
        {
            // TODO:refactor zeroing
            if (player.With(out MoveComponent move))
            {
                move.Zero();
                move.position.Value = new Vector3 {y = 10.0f};
            }
            player.ZeroIfWith<CameraComponent>();
            if (player.With(out HealthProperty health))
                health.Value = 100;
            player.ZeroIfWith<RespawnTimerProperty>();
            player.ZeroIfWith<HitMarkerComponent>();
            player.ZeroIfWith<DamageNotifierComponent>();
            if (player.With(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Grenade, 2);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Molotov, 3);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Shotgun, 4);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.C4, 5);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.Shovel, 6);
            }
        }

        internal override void KillPlayer(Container player)
        {
            base.KillPlayer(player);

            if (player.With(out RespawnTimerProperty respawnTimer))
                respawnTimer.Value = 2_000_000u;
        }

        internal override void Modify(Container session, Container playerToModify, Container commands, uint durationUs)
        {
            base.Modify(session, playerToModify, commands, durationUs);

            if (commands.Without(out InputFlagProperty inputs) || playerToModify.Without(out HealthProperty health) || health.WithoutValue)
                return;

            if (inputs.GetInput(PlayerInput.Suicide) && health.IsAlive)
                KillPlayer(playerToModify);
            if (health.IsDead && playerToModify.With(out RespawnTimerProperty respawn))
            {
                if (respawn.Value > durationUs) respawn.Value -= durationUs;
                else respawn.Value = 0u;
                if (respawn.Value == 0u) SpawnPlayer(playerToModify);
            }
        }
    }
}