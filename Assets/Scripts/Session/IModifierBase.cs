using Session.Player;

namespace Session
{
    internal interface IModifierBase<in TComponent>
    {
        /// <summary>
        ///     Called in FixedUpdate() based on game tick rate
        /// </summary>
        void ModifyChecked(TComponent componentToModify, PlayerCommandsComponent commands);

        /// <summary>
        ///     Called in Update() right after inputs are sampled
        /// </summary>
        void ModifyTrusted(TComponent componentToModify, PlayerCommandsComponent commands);

        void ModifyCommands(PlayerCommandsComponent commandsToModify);
    }
}