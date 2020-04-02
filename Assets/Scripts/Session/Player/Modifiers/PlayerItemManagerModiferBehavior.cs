using Session.Player.Components;

namespace Session.Player.Modifiers
{
    public class PlayerItemManagerModiferBehavior : PlayerModifierBehaviorBase
    {
        public const byte NoneIndex = 0;

        internal override void ModifyChecked(PlayerComponent componentToModify, PlayerCommands commands)
        {
            base.ModifyChecked(componentToModify, commands);
        }

        protected override void SynchronizeBehavior(PlayerComponent componentToApply)
        {
        }
    }
}