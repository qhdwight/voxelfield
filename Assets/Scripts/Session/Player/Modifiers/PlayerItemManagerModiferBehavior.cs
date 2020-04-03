using Session.Player.Components;

namespace Session.Player.Modifiers
{
    public class PlayerItemManagerModiferBehavior : ModifierBehaviorBase<PlayerComponent>
    {
        public const byte NoneIndex = 0;

        public override void ModifyChecked(PlayerComponent componentToModify, PlayerCommandsComponent commands)
        {
            base.ModifyChecked(componentToModify, commands);
        }

        protected override void SynchronizeBehavior(PlayerComponent componentToApply)
        {
        }
    }
}