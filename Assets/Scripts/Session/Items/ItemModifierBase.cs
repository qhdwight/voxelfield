using Session.Player;
using Session.Player.Components;

namespace Session.Items
{
    public enum ItemStatusId
    {
        Unequipping,
        Equipping,
        Idle,
        PrimaryUsing,
        SecondaryUsing
    }

    public enum ItemId
    {
        None,
        TestingRifle,
    }

    public abstract class ItemModifierBase : IModifierBase<ItemComponent>
    {
        public void ModifyTrusted(ItemComponent componentToModify, PlayerCommandsComponent commands)
        {
            
        }

        public void ModifyChecked(ItemComponent componentToModify, PlayerCommandsComponent commands)
        {
            
        }

        public void ModifyCommands(PlayerCommandsComponent commandsToModify)
        {
            
        }
    }
}