using Swihoni.Components;
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
                move.Zero();
            if (player.Has(out CameraComponent camera))
                camera.Zero();
            if (player.Has(out HealthProperty health))
                health.Value = 100;
            if (player.Has(out RespawnTimerProperty respawn))
                respawn.Value = 0.0f;
            if (player.Has(out InventoryComponent inventory))
            {
                inventory.Zero();
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.TestingRifle, 1);
                PlayerItemManagerModiferBehavior.SetItemAtIndex(inventory, ItemId.TestingRifle, 2);
            }
        }

        internal override void KillPlayer(Container player)
        {
            var health = player.Require<HealthProperty>();

            health.Value = 0;
            if (player.Has(out RespawnTimerProperty respawnTimer))
                respawnTimer.Value = 5.0f;
        }

        internal override void Modify(Container playerToModify, Container commands, float duration)
        {
            if (commands.Without(out InputFlagProperty inputs)
             || playerToModify.Without<ServerTag>()
             || playerToModify.Without(out HealthProperty health)) return;

            if (inputs.GetInput(PlayerInput.Suicide))
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