using Input;
using Session.Items;
using Session.Items.Modifiers;
using Session.Player.Components;

namespace Session.Player.Modifiers
{
    public class PlayerItemManagerModiferBehavior : ModifierBehaviorBase<PlayerComponent>
    {
        public const byte NoneIndex = 0;

        public override void ModifyChecked(PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            base.ModifyChecked(componentToModify, commands);
            ItemComponent activeItemComponent = componentToModify.inventory.ActiveItemComponent;
            ItemModifier modifier = ItemManager.Singleton.GetModifier((ItemId) activeItemComponent.id.Value);
            modifier.ModifyChecked(activeItemComponent, commands);
        }

        protected override void SynchronizeBehavior(PlayerComponent componentToApply)
        {
        }

        public override void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
            InputProvider input = InputProvider.Singleton;
            commandsToModify.SetInput(PlayerInput.UseOne, input.GetInput(InputType.UseOne));
            commandsToModify.SetInput(PlayerInput.UseOne, input.GetInput(InputType.UseTwo));
        }
    }
}