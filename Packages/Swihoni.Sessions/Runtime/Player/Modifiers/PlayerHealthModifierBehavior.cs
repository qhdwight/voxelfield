using Swihoni.Components;
using Swihoni.Sessions.Player.Components;

namespace Swihoni.Sessions.Player.Modifiers
{
    public class PlayerHealthModifierBehavior : PlayerModifierBehaviorBase
    {
        public override void ModifyTrusted(Container containerToModify, Container commandsContainer, float duration)
        {
            if (containerToModify.Without<ServerComponent>()
             || commandsContainer.Without(out InputFlagProperty inputs)) return;
            
            if (inputs.GetInput(PlayerInput.Suicide))
            {
                containerToModify.Require<HealthProperty>().Value = 0;
            }
            
            base.ModifyTrusted(containerToModify, commandsContainer, duration);
        }
    }
}